# LeoECS Threads - Поддержка многопоточной обработки.
Поддержка обработки сущностей в несколько системных потоков.

> Проверено на Unity 2020.3 (не зависит от Unity) и содержит asmdef-описания для компиляции в виде отдельных сборок и уменьшения времени рекомпиляции основного проекта.

# Содержание
* [Социальные ресурсы](#Социальные-ресурсы)
* [Установка](#Установка)
    * [В виде unity модуля](#В-виде-unity-модуля)
    * [В виде исходников](#В-виде-исходников)
* [Основные типы](#Основные-типы)
    * [EcsMultiThreadSystem](#EcsMultiThreadSystem)
* [Лицензия](#Лицензия)

# Социальные ресурсы
[![discord](https://img.shields.io/discord/404358247621853185.svg?label=enter%20to%20discord%20server&style=for-the-badge&logo=discord)](https://discord.gg/5GZVde6)

# Установка

## В виде unity модуля
Поддерживается установка в виде unity-модуля через git-ссылку в PackageManager или прямое редактирование `Packages/manifest.json`:
```
"com.leopotam.ecs-threads": "https://github.com/Leopotam/ecs-threads.git",
```
По умолчанию используется последняя релизная версия. Если требуется версия "в разработке" с актуальными изменениями - следует переключиться на ветку `develop`:
```
"com.leopotam.ecs-threads": "https://github.com/Leopotam/ecs-threads.git#develop",
```

## В виде исходников
Код так же может быть склонирован или получен в виде архива со страницы релизов.

# Основные типы

## EcsMultiThreadSystem
`EcsMultiThreadSystem` - ECS-система, позволяющая распараллеливать обработку сущностей в фильтре на определенное количество потоков.
```c#
struct ThreadComponent {
    public float A;
    public float B;
    public float C;
    public float D;
    public float E;
    public float F;
    public float G;
    public float H;
    public float I;
    public float J;
    public float Result;
}

sealed class ThreadTestSystem : EcsMultiThreadSystem<EcsFilter<ThreadComponent>> {
    EcsWorld _world;
    EcsFilter<ThreadComponent> _filter;

    // Метод должен вернуть фильтр, который будет источником сущностей для обработки.
    protected override EcsFilter<ThreadComponent> GetFilter () {
        return _filter;
    }

    // Метод возвращает минимальное количество сущностей,
    // после которого может происходить разделение обработки
    // на несколько потоков.
    protected override int GetMinJobSize () {
        return 1000;
    }

    // Метод возвращает максимальное количество потоков,
    // которые будут использоваться для распараллеливания.
    protected override int GetThreadsCount () {
        return System.Environment.ProcessorCount - 1;
    }

    // Метод возвращает обработчик, содержащий логику обработки сущностей.
    protected override EcsMultiThreadWorker GetWorker () {
        return Worker;
    }

    void Worker (EcsMultiThreadWorkerDesc workerDesc) {
        foreach (var idx in workerDesc) {
            ref var c = ref workerDesc.Filter.Get1(idx);
            c.Result = (float) System.Math.Sqrt (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Sin (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Cos (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Tan (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Log10 (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Sqrt (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Sin (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Cos (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Tan (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
            c.Result = (float) System.Math.Log10 (c.A + c.B + c.C + c.D + c.E + c.F + c.G + c.H + c.I + c.J);
        }
    }
}
```

# Лицензия
Фреймворк выпускается под двумя лицензиями, [подробности тут](./LICENSE.md).

В случаях лицензирования по условиям MIT-Red не стоит расчитывать на
персональные консультации или какие-либо гарантии.