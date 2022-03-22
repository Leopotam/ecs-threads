// ----------------------------------------------------------------------------
// The Proprietary or MIT-Red License
// Copyright (c) 2012-2022 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading;

#if ENABLE_IL2CPP
// Unity IL2CPP performance optimization attribute.
namespace Unity.IL2CPP.CompilerServices {
    enum Option {
        NullChecks = 1,
        ArrayBoundsChecks = 2
    }

    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    class Il2CppSetOptionAttribute : Attribute {
        public Option Option { get; private set; }
        public object Value { get; private set; }

        public Il2CppSetOptionAttribute (Option option, object value) { Option = option; Value = value; }
    }
}
#endif

namespace Leopotam.Ecs.Threads {
#if ENABLE_IL2CPP
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
    [Unity.IL2CPP.CompilerServices.Il2CppSetOption (Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
#endif
    public abstract class EcsMultiThreadSystem<T> : IEcsPreInitSystem, IEcsPostDestroySystem, IEcsRunSystem where T : EcsFilter {
        EcsMultiThreadWorkerDesc[] _descs;
        ManualResetEvent[] _syncs;
        EcsMultiThreadWorkerDesc _localDesc;
        T _filter;
        EcsMultiThreadWorker _worker;
        int _minJobSize;
        int _threadsCount;

        void IEcsPreInitSystem.PreInit () {
            _filter = GetFilter ();
            _worker = GetWorker ();
            _minJobSize = GetMinJobSize ();
            _threadsCount = GetThreadsCount ();
#if DEBUG
            if (_filter == null) {
                throw new Exception ("GetFilter() returned null");
            }
            if (_minJobSize < 1) {
                throw new Exception ("GetMinJobSize() returned invalid value");
            }
            if (_threadsCount < 1) {
                throw new Exception ("GetThreadsCount() returned invalid value");
            }
#endif
            _localDesc = new EcsMultiThreadWorkerDesc (_filter);
            _descs = new EcsMultiThreadWorkerDesc[_threadsCount];
            _syncs = new ManualResetEvent[_threadsCount];
            for (var i = 0; i < _descs.Length; i++) {
                var desc = new EcsMultiThreadWorkerDesc (_filter);
                desc.Thread = new Thread (ThreadProc);
                desc.Thread.IsBackground = true;
#if DEBUG
                desc.Thread.Name = string.Format ("ECS-{0:X}-{1}", GetHashCode (), i);
#endif
                desc.HasWork = new ManualResetEvent (false);
                desc.WorkDone = new ManualResetEvent (true);
                desc.Worker = _worker;
                _descs[i] = desc;
                _syncs[i] = desc.WorkDone;
                desc.Thread.Start (desc);
            }
        }

        void IEcsPostDestroySystem.PostDestroy () {
            for (var i = 0; i < _descs.Length; i++) {
                var desc = _descs[i];
                _descs[i] = null;
                desc.Thread.Interrupt ();
                desc.Thread.Join (10);
                _syncs[i].Close ();
                _syncs[i] = null;
            }
            _filter = null;
            _worker = null;
            _localDesc = null;
        }

        void IEcsRunSystem.Run () {
            var count = _filter.GetEntitiesCount ();
            if (count > 0) {
                var processed = 0;
                var jobSize = count / (_threadsCount + 1);
                int workersCount;
                if (jobSize > _minJobSize) {
                    workersCount = _threadsCount + 1;
                } else {
                    workersCount = count / _minJobSize;
                    jobSize = _minJobSize;
                }
                for (var i = 0; i < workersCount - 1; i++) {
                    var desc = _descs[i];
                    desc.IndexFrom = processed;
                    processed += jobSize;
                    desc.IndexTo = processed;
                    desc.WorkDone.Reset ();
                    desc.HasWork.Set ();
                }
                // local worker.
                _localDesc.IndexFrom = processed;
                _localDesc.IndexTo = count;
                _worker (_localDesc);

                // sync workers back to ecs thread.
                for (var i = 0; i < _syncs.Length; i++) {
                    _syncs[i].WaitOne ();
                }
            }
        }

        void ThreadProc (object rawDesc) {
            var desc = (EcsMultiThreadWorkerDesc) rawDesc;
            try {
                while (Thread.CurrentThread.IsAlive) {
                    desc.HasWork.WaitOne ();
                    desc.HasWork.Reset ();
                    desc.Worker (desc);
                    desc.WorkDone.Set ();
                }
            } catch {
                // ignored
            }
        }

        /// <summary>
        /// Source filter for processing entities from it.
        /// </summary>
        protected abstract T GetFilter ();

        /// <summary>
        /// Custom processor of received entities.
        /// </summary>
        protected abstract EcsMultiThreadWorker GetWorker ();

        /// <summary>
        /// Minimal amount of entities to process by one worker. Will be called only once.
        /// </summary>
        protected abstract int GetMinJobSize ();

        /// <summary>
        /// How many threads should be used by this system. Will be called only once.
        /// </summary>
        protected abstract int GetThreadsCount ();

        public delegate void EcsMultiThreadWorker (EcsMultiThreadWorkerDesc workerDesc);

        public sealed class EcsMultiThreadWorkerDesc {
            public readonly T Filter;
            internal Thread Thread;
            internal ManualResetEvent HasWork;
            internal ManualResetEvent WorkDone;
            internal EcsMultiThreadWorker Worker;
            internal int IndexFrom;
            internal int IndexTo;

            internal EcsMultiThreadWorkerDesc (T filter) {
                Filter = filter;
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator () {
                return new Enumerator (IndexFrom, IndexTo);
            }

            public struct Enumerator {
                readonly int _count;
                int _idx;

                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                internal Enumerator (int from, int to) {
                    _idx = from - 1;
                    _count = to;
                }

                public int Current {
                    [MethodImpl (MethodImplOptions.AggressiveInlining)]
                    get => _idx;
                }

                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                public bool MoveNext () {
                    return ++_idx < _count;
                }
            }
        }
    }
}