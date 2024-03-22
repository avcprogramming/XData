// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/
/// Отрывок кода чисто для примера для xData

// Ignore Spelling: rgb

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using static System.String;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Bricscad.EditorInput;
using Teigha.Runtime;
using Bricscad.Windows;
using CadApp = Bricscad.ApplicationServices.Application;
using Ge = Teigha.Geometry;
using Rt = Teigha.Runtime;
using Db = Teigha.DatabaseServices;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Ge = Autodesk.AutoCAD.Geometry;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Rt = Autodesk.AutoCAD.Runtime;
#endif

namespace AVC
{
  /// <summary>
  /// Описание поверхности солида для показа в дереве чертежа и отображения свойств в панели-наследнике Props.
  /// Отрывок кода чисто для примера для xData
  /// </summary>
  internal class
  AvcSolidFace 
  {
    /// <summary>
    /// Фасад, Тыл, Торец
    /// </summary>
    public enum
    Direction
    { Other = 0, Front, Rear, End }

    #region Properties

    /// <summary>
    /// Идентификатор солида
    /// </summary>
    public ObjectId
    OwnerId
    { get; set; }

    /// <summary>
    /// Идентификатор поверхности в структуре солида
    /// </summary>
    public SubentityId
    Id
    { get; set; }

    public FullSubentityPath
    Path => new(new ObjectId[] { OwnerId }, Id);

    /// <summary>
    /// Идентификатор поверхности (int)SubentityId 
    /// </summary>
    public int
    IntId
    {
      get => (int)Id.IndexPtr;
      set => Id = new SubentityId(SubentityType.Face, (IntPtr)value);
    }

    /// <summary>
    /// Есть не сохраненные изменения материала или цвета
    /// </summary>
    public bool
    Updated
    { get; set; }


    /// <summary>
    /// Площадь. В миллиметровых чертежах - в метрах квадратных. 
    /// Округлено для сравнения на ==. 
    /// Используется как идентификатор при присвоениях материала.
    /// NaN используется как метка "для всех прочих".
    /// </summary>
    public double
    Area
    { get; set; }

    private string _materialName = "";

    /// <summary>
    /// Имя материала покрытия. Храним отдельно от AvcMaterial чтоб иметь возможность записывать в палитре новые матриалы как текст.
    /// </summary>
    public string
    MaterialName
    {
      get => _materialName;
      set { if (_materialName != value) Updated = true; _materialName = value; }
    }

     private string _colorName = "";

    /// <summary>
    /// Цвет поверхности солида (только если он отличается от цвета солида)
    /// </summary>
    public string
    ColorName
    {
      get => _colorName;
      set { if (_colorName != value) Updated = true; _colorName = value; RGB = 0; }
    }

    public int
    RGB
    { get; private set; } = 0;

    /// <summary>
    /// Направление данной поверхности: фасад, тыл, торец
    /// </summary>
    public Direction
    Dir
    { get; set; } = Direction.Other;

    private bool _frontColorMark = false;
    /// <summary>
    /// Поверхность помечена цветом как фасадная
    /// </summary>
    public bool
    FrontColorMark
    {
      get => _frontColorMark;
      set { if (_frontColorMark != value) Updated = true; _frontColorMark = value; }
    }

    /// <summary>
    /// Направление поверхности как у торцев (перпендикуляр к фасаду)
    /// </summary>
    public bool
    IsEnd
    {
      get => Dir == Direction.End;
      set { if (value) Dir = Direction.End; else Dir = Direction.Other; }
    }

    /// <summary>
    /// Экземпляр используется для хранения значения материала для "всех прочих" перечисленных в списке поверхностей
    /// </summary>
    // internal bool IsAllOther { get { return !IsMain && IsNull && double.IsNaN(Area4Calc); } }

    /// <summary>
    /// Имя цвета без имени каталога цветов
    /// </summary>
    public string
    ColorKey
    {
      get
      {
        if (IsNullOrEmpty(ColorName)) return "";
        int pos = ColorName.IndexOf('$');
        if (pos < 0 && pos >= ColorName.Length - 1) return ColorName;
        return ColorName.Substring(pos + 1);
      }
    }

    /// <summary>
    /// Нет идентификатора поверхности
    /// </summary>
    public bool
    IsNull => Id == new SubentityId();

    /// <summary>
    /// направление поверхности фронт или тыл
    /// </summary>
    public bool
    IsMain => Dir == Direction.Front || Dir == Direction.Rear;

    public const string
    FieldNamePrefixFace = "Face";

    #endregion

    public
    AvcSolidFace()
    { }

    public AvcSolidFace
    Clone() => MemberwiseClone() as AvcSolidFace;

  }
}
