// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/
// Отрывок кода чисто для примера для xData

using static System.Math;
using static System.String;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.Runtime;
using Bricscad.Windows;
using CadApp = Bricscad.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AVC
{
  /// <summary>
  /// Описание торцев солида, покрытых кромочным материалом 
  /// Отрывок кода чисто для примера для xData
  /// </summary>
  internal class 
  AvcSolidBanding : AvcSolidFace
  {

    #region Properties

    /// <summary>
    /// Номер кромки по порядку обхода детали
    /// </summary>
    public int
    Index
    { get; set; } = 0;

    /// <summary>
    /// Обозначение кромки используемое как ее название. Одна буква или индекс или длина
    /// </summary>
    public string
    Letter
    { get; set; } = "";

    /// <summary>
    /// Длина торца в единицах чертежа (не приводится к метрам!)
    /// </summary>
    public double 
    Length { get; set; }

    /// <summary>
    /// Угол наклона пилы от вертикали для данного торца, если торец плоский. 0 - вертикальный. меньше ноля - снизу от фасада. Радианы
    /// </summary>
    public double 
    Angle { get; set; }

    /// <summary>
    /// Начало ребра (у выложенной детали)
    /// </summary>
    public Point2d 
    Start { get; set; }

    public Point2d 
    End { get; set; }

    public double 
    AngleInGrad => Round(Angle * 180 / PI, 1); 

#endregion

    public
    AvcSolidBanding()
    { }

    /// <summary>
    /// Поменять местами Start и End
    /// </summary>
    public void 
    Revers()
    {
      Point2d end = Start; Start = End; End = end;
    }
    
    public override int 
    GetHashCode() =>
      Id.GetHashCode() ^ Area.GetHashCode() ^ ColorName.GetHashCode() 
      ^ ColorName.GetHashCode() ^ Length.GetHashCode() ^ Angle.GetHashCode();
 
    public override bool 
    Equals(object obj)
    {
      if (obj is AvcSolidBanding avc)
      {
        if (Id != avc.Id) return false; // в старых BricsCAD не работает Equals
        if (!Area.Equals(avc.Area)) return false;
        if (!MaterialName.Equals(avc.MaterialName)) return false;
        if (!ColorName.Equals(avc.ColorName)) return false;
        if (!Length.Equals(avc.Length)) return false;
        return Angle.Equals(avc.Angle);
      }
      return false;
    }

    public static bool 
    operator ==(AvcSolidBanding a, AvcSolidBanding b)
    {
      if (a is null) return b is null;
      return a.Equals(b);
    }

    public static bool 
    operator !=(AvcSolidBanding a, AvcSolidBanding b) => !(a == b);
    

  }
}
