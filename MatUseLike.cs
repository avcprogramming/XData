// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Reflection;

namespace AVC
{
  /// <summary>
  /// Назначение материала
  /// </summary>
  [Obfuscation(Exclude = true, Feature = "renaming")]
  internal enum
  MatUseLike
  {
    /// <summary>
    /// Неизвестное назначение. Сохранено для совместимости со старыми версиями
    /// </summary>
    Unknown = 0, 

    /// <summary>
    /// Неизвестно назначение или массив. 
    /// Учитываем как массив (по объему деталей)
    /// </summary>
    Volume = 0,

    /// <summary>
    /// Листовой. Учет по площади фасада деталей
    /// </summary>
    Sheet = 1,

    /// <summary>
    /// Прокат, погонаж, прут. Учет по длине деталей
    /// </summary>
    Rod = 2,

    /// <summary>
    /// Покрытие. Не для деталей, а для их поверхностей. Учет по площади грани.
    /// </summary>
    Cover = 3,

    /// <summary>
    /// Кромка. Не для деталей, а для их поверхностей. Учет по длине торца.
    /// </summary>
    Banding = 4,

    Varies = 5
  }
}
