// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/
// Отрывок кода чисто для примера для xData

using System.IO;
using System.Collections.Generic;
using static System.String;
#if BRICS
using Teigha.DatabaseServices;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.DatabaseServices;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Rt = Autodesk.AutoCAD.Runtime;
#endif

namespace AVC
{

  /// <summary>
  /// Отрывок кода чисто для примера для xData
  /// </summary>
  internal static class
  DatabaseExt
  {
    private static readonly char[]
    _specSymbols = new List<char>(Path.GetInvalidFileNameChars()) { '`', ';', ',', '=' }.ToArray();

    /// <summary>
    /// Заменить запрещенные символы в строке имени слоя, блока, материала
    /// </summary>
    public static string
    ValidName(this string newName, string rep = "-", bool trim = true)
    {
      if (newName is null) return null;
      if (trim) newName = newName.Trim();
      return Join(rep, newName.Split(_specSymbols));
    }

    /// <summary>
    /// Получить Id Модели
    /// </summary>
    public static ObjectId
    GetModelId(this Database db) => SymbolUtilityServices.GetBlockModelSpaceId(db);

    public static bool
    IsModel(this ObjectId spaceId) =>
      !spaceId.IsNull && spaceId == SymbolUtilityServices.GetBlockModelSpaceId(spaceId.Database);

    /// <summary>
    /// Получить Модель для записи
    /// </summary>
    public static BlockTableRecord
    GetModel(this Database db, Transaction tr) =>
      tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite) as BlockTableRecord;

    /// <summary>
    /// Единицы чертежа Insunits. Если не заданы - вернет дюймы или миллиметры в зависимости от формата вывода Lunits
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static UnitsValue
    GetUnits(this Database db)
    {
      UnitsValue ret = db.Insunits;
      if (ret != UnitsValue.Undefined) return ret;
      if (db.Lunits > 2) // дробные форматы
        return UnitsValue.Inches;
      else
        return UnitsValue.Millimeters;
    }

    public static bool
    MM(this Database db) => GetUnits(db) == UnitsValue.Millimeters;

    public static bool
    Inch(this Database db) => GetUnits(db) == UnitsValue.Inches;
 

  }
}
