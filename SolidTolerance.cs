// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/
// Отрывок кода чисто для примера для xData

// Ignore Spelling: Equals tol Eq

using System;
using static System.Math;
using System.Globalization;
#if BRICS
using Teigha.Geometry;
#else
using Autodesk.AutoCAD.Geometry;
#endif

namespace AVC
{
  /// <summary>
  /// SolidTolerance - настройки точности вычислений для работы с солидами
  /// Отрывок кода чисто для примера для xData
  /// </summary>
  internal static class STol
  {

    public static double 
    EqVector = PI / 1800; // 0.1 градуса

    /// <summary>
    /// Точность сравнения единичных векторов и очень коротких отрезков
    /// </summary>
    public static Tolerance 
    UnitVector => new (EqVector, ZeroSize); 

    public const byte 
    maxDecimalDigit = 8;

    public const byte 
    minDecimalDigit = 5;

    /// <summary>
    /// Минимальный размер, который считаем равным 0
    /// </summary>
    public const double 
    ZeroSize = 0.0000001;

    public static double 
    ZeroArea => Pow(ZeroSize, 2);

    public static double 
    ZeroVolume => Pow(ZeroSize, 3);

    /// <summary>
    /// Приблизительно равно нулю (мельче чем ZeroSize)
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static bool 
    ApproxZero(this double size) => Abs(size) < ZeroSize;

    /// <summary>
    /// Округление до стольких же знаков после запятой как у EqualPoint. Если EqualPoint > 1 - округлит до целых
    /// </summary>
    public static double 
    ApproxLike(this double x, Tolerance tol)
    {
      if (double.IsNaN(x) || double.IsInfinity(x) || x == 0.0) return x;
      if (x.ApproxZero()) return 0.0;
      byte dig = (byte)Ceiling(-Log10(tol.EqualPoint));
      if (dig < 0) dig = 0;
      return Round(x, dig);
    }

    public static bool 
    IsZero(this Vector3d v) => Abs(v.X) < ZeroSize && Abs(v.Y) < ZeroSize && Abs(v.Z) < ZeroSize;

  }
}
