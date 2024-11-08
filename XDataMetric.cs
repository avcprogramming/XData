// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

// Ignore Spelling: Calc

using System.ComponentModel;
using static System.Math;
using System;
#if BRICS
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Db = Teigha.DatabaseServices;
#else
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Db = Autodesk.AutoCAD.DatabaseServices;
#endif

namespace AVC
{

  /// <summary>
  /// Данные о солиде по результатам его обмера (анализа геометрии).
  /// Позволяют определить технологии, необходимые для изготовления детали.
  /// А так же флаг блокировки метрики от обмера (для ручного ввода метрики).
  /// </summary>
  [Flags]
  internal enum
  SolidMetricEnum
  {
    /// <summary>
    /// Деталь требует обработки с фасадной стороны (при выкладке фасадом вниз - это сверления снизу, фрезеровка снизу). 
    /// Без учета наклона торцев и 3d-поверхностей.
    /// </summary>
    FrontProcessing = 1 << 0,
    /// <summary>
    /// Деталь требует обработки с задней стороны (при выкладке фасадом вниз - это сверления сверху, фрезеровка сверху). 
    /// Без учета наклона торцев и 3d-поверхностей.
    /// </summary>
    RearProcessing = 1 << 1,
    /// <summary>
    /// Заблокирована запись метрики в XDataMetric при автоматическом обмере. 
    /// То есть обмер будет сделан, но результаты в XDataMetric записаны не будут
    /// </summary>
    Blocked = 1 << 2,
    /// <summary>
    /// Объем точно совпадает с объемом габаритного бокса
    /// </summary>
    IsBox = 1 << 3,
    /// <summary>
    /// не все поверхности плоские
    /// </summary>
    HasNotFlatFaces = 1 << 4,
    /// <summary>
    /// есть внутренние контуры
    /// </summary>
    HasHoles = 1 << 5,
    /// <summary>
    /// имеет поверхности более сложные чем может сделать 2D ЧПУ 
    /// </summary>
    Has3dFaces = 1 << 6,
    /// <summary>
    /// главная плоскость - это прямоугольник
    /// </summary>
    FrontIsRectangle = 1 << 7,
    /// <summary>
    /// есть поверхности с материалом отличным от материала солида (покрытие)
    /// </summary>
    HasCoveredFaces = 1 << 8,
    /// <summary>
    /// фасадная поверхность задана цветом или покрытием или верхняя. Тело нельзя перевернуть, даже если оно симметрично
    /// </summary>
    HasPriorFace = 1 << 9,
    /// <summary>
    /// Круглая труба (цилиндр или тор)
    /// </summary>
    IsPipe = 1 << 10,

    TwoSideProcessing = FrontProcessing | RearProcessing
  }

  /// <summary>
  /// Хранение имен солидов и др объектов чертежа в XData
  /// </summary>
  internal class
  XDataMetric : XDataMan
  {
    [Browsable(false)]
    public override string
    XDAppName => "AVCMetric";

    /// <summary>
    /// если numChanges не совпадает с солидом - значит солид уже изменен и надо перемерять
    /// </summary>
    [Browsable(false)]
    public int
    NumChanges
    { get; set; }

    /// <summary>
    /// длина
    /// </summary>
    public double
    Length
    { get; set; }

    /// <summary>
    /// ширина
    /// </summary>
    public double
    Width
    { get; set; }

    /// <summary>
    /// толщина
    /// </summary>
    public double
    Thickness
    { get; set; }

    /// <summary>
    /// показатель асимметричности: расстояние от центра бокса до центра масс. 
    /// Позволяет выявить смещения отверстий и др. минимальные отличия при одинаковом объеме
    /// </summary>
    public double
    Asymmetry
    { get; set; }

    /// <summary>
    /// текстовое представление вектора 3мя символами
    /// </summary>
    public string
    AsymmetryStr
    { get; set; }

    /// <summary>
    /// технология изготовления исходя из геометрии. пока только БОКС и Развертка
    /// </summary>
    public string
    Technology
    { get; set; }

    /// <summary>
    /// Данные о солиде по результатм его обмера (анализа геометрии).
    /// Позволяют определить технологии, необходимые для изготовления детали.
    /// А так же флаг блокировки метрики от обмера (для ручного ввода метрики).
    /// </summary>
    public SolidMetricEnum
    Flags
    { get; set; }

    /// <summary>
    /// Объем. в миллиметровом чертеже должен быть пересчитан в кубометры
    /// </summary>
    public double
    Volume4Calc
    { get; set; }

    /// <summary>
    /// площадь фасадной плоской грани (или сумма компланарных регионов). 
    /// В миллиметровых чертежах - в метрах квадратных
    /// </summary>
    public double
    Area4Calc
    { get; set; }

    /// <summary>
    /// Наружный периметр наибольшей плоской грани.
    /// В миллиметровых чертежах - в метрах
    /// </summary>
    public double
    Perimeter4Calc
    { get; set; }

    /// <summary>
    /// Количество граней (поверхностей) солида. Не редактируемое поле в Палитре AVC, даже когда обмер заблокирован
    /// </summary>
    public int
    FaceCount
    { get; set; }

    /// <summary>
    /// Вес на основе плотности материала
    /// </summary>
    public double
    Weight
    { get; set; }

    /// <summary>
    /// Цена. На основе данных о материале, покрытиях и кромках
    /// </summary>
    public double
    Cost
    { get; set; }

    /// <summary>
    /// Матрица выкладки данного солида в XY по всем правилам команды LAY
    /// </summary>
    public Matrix3d
    Lay
    { get; set; }

    /// <summary>
    /// Метрика не обмерена. Или нулевой объем (у заблокированных метрик объем игнорируем)
    /// </summary>
    public bool
    IsNull => NumChanges < 0 || !Blocked && Volume4Calc == 0;

    /// <summary>
    /// Заблокирована запись метрики в XDataMetric при автоматическом обмере. 
    /// Пользователь может вручную править данные метрики.
    /// </summary>
    public bool
    Blocked => (Flags & SolidMetricEnum.Blocked) != 0;

    public
    XDataMetric() : base()
    { NumChanges = -1; }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах.
    /// Вернет пустую метрику XDataMetric.IsNull == true, если NumChanges солида изменился по сравнению с XDataMetric.NumChanges
    /// </summary>
    public
    XDataMetric(ObjectId id, Transaction tr) : base(id, tr)
    {
      if (IsNull) return; // метрики нет
      if (id.IsNull || id.Database is null) { Clear(); return; }
      Solid3d solid = tr.GetObject(id, OpenMode.ForRead) as Solid3d;
      //if (solid is not null) Cns.DebugInfo($"         NumChanges: solid = {solid.NumChanges}, XDataMetric = {NumChanges}");
      if (solid is null || (!Blocked && solid.NumChanges != NumChanges)) // метрика есть, но устарела
        Clear();
    }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах. 
    /// Вернет пустую метрику XDataMetric.IsNull == true, если NumChanges солида изменился по сравнению с XDataMetric.NumChanges
    /// </summary>
    public
    XDataMetric(DBObject obj) : base(obj)
    {
      if (!Blocked && obj is Solid3d solid && solid.NumChanges != NumChanges)
        Clear();
    }

    /// <summary>
    /// Для записи xData используем DBObject.XData = Buffer, 
    /// для чтения Buffer = DBObject.GetXDataForApplication(XDAppName)
    /// </summary>
    public override ResultBuffer
    Buffer
    {
      get
      {
        ResultBuffer buffer = NewXData(); // Первое поле - AppName
        buffer.AddData(NumChanges);
        buffer.AddData(Length);
        buffer.AddData(Width);
        buffer.AddData(Thickness);
        buffer.AddData(Volume4Calc);
        buffer.AddData(Asymmetry);
        buffer.AddData(AsymmetryStr);
        buffer.AddData(Technology);
        buffer.AddData((int)Flags);
        buffer.AddData(Area4Calc);
        buffer.AddData(Perimeter4Calc);
        buffer.AddData(FaceCount);
        buffer.AddData(Weight);
        buffer.AddData(Cost);
        buffer.AddData(Lay);
        return buffer;
      }
      set
      {
        if (value is null) { Clear(); return; }
        TypedValue[] arr = value.AsArray();
        if (arr.Length >= 2 && arr[1].Value is int i) NumChanges = i;
        if (arr.Length >= 3 && arr[2].Value is double d1) Length = d1;
        if (arr.Length >= 4 && arr[3].Value is double d2) Width = d2;
        if (arr.Length >= 5 && arr[4].Value is double d3) Thickness = d3;
        if (arr.Length >= 6 && arr[5].Value is double d4) Volume4Calc = d4;
        if (arr.Length >= 7 && arr[6].Value is double d5) Asymmetry = d5;
        if (arr.Length >= 8 && arr[7].Value is string s1) AsymmetryStr = s1;
        if (arr.Length >= 9 && arr[8].Value is string s2) Technology = s2;
        if (arr.Length >= 10 && arr[9].Value is int f) Flags = (SolidMetricEnum)f;
        if (arr.Length >= 11 && arr[10].Value is double d6) Area4Calc = d6;
        if (arr.Length >= 12 && arr[11].Value is double d7) Perimeter4Calc = d7;
        if (arr.Length >= 13 && arr[12].Value is int c) FaceCount = c;
        if (arr.Length >= 14 && arr[13].Value is double d8) Weight = d8;
        if (arr.Length >= 15 && arr[14].Value is double d9) Cost = d9;
        Lay = arr.GetMatrix(15);
      }
    }

    public override void
    Clear()
    {
      NumChanges = -1;
      Length = 0;
      Width = 0;
      Thickness = 0;
      Volume4Calc = 0;
      Asymmetry = 0;
      AsymmetryStr = "";
      Technology = "";
      Flags = 0;
      Area4Calc = 0;
      Perimeter4Calc = 0;
      FaceCount = 0;
      Weight = 0;
      Cost = 0;
      Lay = new Matrix3d();
    }

    /// <summary>
    /// Рекомендуется удалять метрику у модифицированных солидов, так как иногда не срабатывает проверка NumChanges
    /// </summary>
    public static void
    Clear(Solid3d solid, Database db, Transaction tr)
    {
      XDataMetric xm = new();
      xm.ClearXData(solid, db, tr);
      XDataCovers xc = new();
      xc.ClearXData(solid, db, tr);
      XDataBandings xb = new();
      xb.ClearXData(solid, db, tr);
    }

    /// <summary>
    /// Более производительное сравнение, чем сравнение буферов
    /// </summary>
    public override bool
    Equals(object obj) =>
      obj is XDataMetric xd
      && xd.NumChanges == NumChanges
      && xd.FaceCount == FaceCount
      && Abs(xd.Perimeter4Calc - Perimeter4Calc) < STol.ZeroSize
      && xd.Technology == Technology
      && Abs(xd.Thickness - Thickness) < STol.ZeroSize
      && Abs(xd.Volume4Calc - Volume4Calc) < STol.ZeroVolume
      && Abs(xd.Length - Length) < STol.ZeroSize
      && Abs(xd.Area4Calc - Area4Calc) < STol.ZeroArea
      && Abs(xd.Asymmetry - Asymmetry) < STol.ZeroSize
      && xd.AsymmetryStr == AsymmetryStr
      && xd.Flags == Flags
      && Abs(xd.Cost - Cost) < 0.01
      && Abs(xd.Weight - Weight) < STol.ZeroVolume
      && xd.Lay == Lay;

    public static bool
    operator ==(XDataMetric a, XDataMetric b) => a is null ? b is null : a.Equals(b);

    public static bool
    operator !=(XDataMetric a, XDataMetric b) => a is null ? b is not null : !a.Equals(b);

    public override int
    GetHashCode() => base.GetHashCode();

  }
}
