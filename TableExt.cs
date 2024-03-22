// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/
// Отрывок кода чисто для примера для xData

using System.Collections.Generic;
using static System.String;
#if BRICS
using Teigha.DatabaseServices;
using Teigha.Runtime;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Rt = Autodesk.AutoCAD.Runtime;
#endif

namespace AVC
{
  /// <summary>
  /// Вспомогательные методы-хелперы работы с таблицами AutoCAD
  /// Отрывок кода чисто для примера для xData
  /// </summary>
  internal static class
  TableExt
  {

    public static readonly RXClass
    dbTable = RXObject.GetClass(typeof(Table));

    public static bool
    IsTable(this ObjectId id) =>
#if BRICS
      id.ObjectClass.Name == "AcDbTable";
#else
      id.ObjectClass == RXClass.GetClass(typeof(Table)); // в BricsCAD возвращает null
#endif

    public static void
    InvisibleBorders(Table table, int row, int col)
    {
      table.Cells[row, col].Borders.Left.IsVisible = false;
      table.Cells[row, col].Borders.Right.IsVisible = false;
      table.Cells[row, col].Borders.Bottom.IsVisible = false;
      table.Cells[row, col].Borders.Top.IsVisible = false;
    }

    public static List<ObjectId>
    AllTables(this Database db)
    {
      List<ObjectId> tableIds = new();
      for (long i = db.BlockTableId.Handle.Value; i < db.Handseed.Value; i++)
      {
        if (db.TryGetObjectId(new Handle(i), out ObjectId id) &&
          !id.IsNull && !id.IsErased && id.IsValid && id.IsTable())
          tableIds.Add(id);
      }
      return tableIds;
    }

    /// <summary>
    /// найти таблицу с заголовком title на всех листах активного документа
    /// извлекает таблицу для записи
    /// </summary>
    /// <param name="findFirst">c первого или последнего (по tab ордеру) искать</param>
    public static Table
    FindTable(string title, Transaction tr, bool findFirst)
    {
      Table ret = null;
      int tab = 0;
      using (DBDictionary layoutDict = tr.GetObject(HostApplicationServices.WorkingDatabase.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary)
        foreach (DBDictionaryEntry de in layoutDict)
          using (Layout ltr = tr.GetObject(de.Value, OpenMode.ForRead) as Layout)
          using (BlockTableRecord btr = tr.GetObject(ltr.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord)
            foreach (ObjectId objId in btr)
              if (!objId.IsErased && objId.IsTable())
              {
                Table table = tr.GetObject(objId, OpenMode.ForWrite, false, true) as Table;
                if (table.Cells[0, 0].TextString.IndexOf(title) != -1 &&
                    (ret is null || (findFirst && tab > ltr.TabOrder) || (!findFirst && tab < ltr.TabOrder)))
                {
                  ret = table;
                  tab = ltr.TabOrder;
                }
              }
      return ret;
    }

    /// <summary>
    /// найти таблицу с заголовком title на листе layoutID. 
    /// работает в отдельной транзакции
    /// </summary>
    /// <param name="title"></param>
    /// <param name="layoutID"></param>
    /// <returns></returns>
    public static ObjectId
    FindTable(string title, ObjectId layoutID)
    {
      if (IsNullOrEmpty(title) || layoutID.IsNull) return new ObjectId();
      Database db = layoutID.Database;
      if (db is null) return new ObjectId();
      using (Transaction tr = db.TransactionManager.StartTransaction())
      {
        using (Layout lo = tr.GetObject(layoutID, OpenMode.ForRead) as Layout)
        using (BlockTableRecord acBlkTblRec = tr.GetObject(lo.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord)
          foreach (ObjectId objId in acBlkTblRec)
            if (objId.IsTable())
              using (Table table = tr.GetObject(objId, OpenMode.ForRead) as Table)
                if (table.Cells[0, 0].TextString.IndexOf(title) != -1 || table.Cells[0, 1].TextString.IndexOf(title) != -1)
                  return objId;
        tr.Commit();
      }
      return new ObjectId();
    }

    /// <summary>
    /// Имена всех стилей таблиц в заданной db
    /// </summary>
    public static List<string>
    TableStyles(Database db)
    {
      List<string> ret = new();
      if (db is null || db.IsDisposed) return ret;
      using Transaction tr = db.TransactionManager.StartTransaction();
      DBDictionary styles = tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
      if (styles is null) return ret;
      foreach (DBDictionaryEntry ID in styles)
      {
        TableStyle style = tr.GetObject(ID.Value, OpenMode.ForRead) as TableStyle;
        if (styles != null)
          ret.Add(style.Name);
      }
      tr.Commit();
      return ret;
    }

    public static void
    UnlockContent(this Table tb, int r, int c)
    {
      if (tb.Cells[r, c].State is null)
        tb.Cells[r, c].State = CellStates.None;
      else
        tb.Cells[r, c].State = tb.Cells[r, c].State.Value & ~(CellStates.ContentLocked | CellStates.ContentModifiedAfterUpdate | CellStates.ContentReadOnly | CellStates.Linked);
    }

    public static void
    UnlockContent(this CellRange range)
    {
      if (range is null || range.IsNull) return;
      CellRange save = CellRange.Create(range.ParentTable, range.TopRow, range.LeftColumn, range.BottomRow, range.RightColumn); // BricsCAD испохабит range
      if (save.State is null)
        save.State = CellStates.None;
      else
        save.State = save.State.Value & ~(CellStates.ContentLocked | CellStates.ContentModifiedAfterUpdate | CellStates.ContentReadOnly | CellStates.Linked);
    }

    public static bool
    ContentLocked(this Table tb, int r, int c)
    {
      if (tb.Cells[r, c].State is null) return false;
      return tb.Cells[r, c].State.Value.HasFlag(CellStates.ContentLocked) || tb.Cells[r, c].State.Value.HasFlag(CellStates.ContentReadOnly) || tb.Cells[r, c].State.Value.HasFlag(CellStates.Linked);
    }

    public static bool
    FormatLocked(this Table tb, int r, int c)
    {
      if (tb.Cells[r, c].State is null) return false;
      return tb.Cells[r, c].State.Value.HasFlag(CellStates.FormatLocked);
    }

    /// <summary>
    /// первая строка таблицы
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    public static string
    GetTitle(this Table table)
    {
      if (table is null || table.IsErased || table.Columns.Count == 0 || table.Rows.Count == 0)
        return "";

      string ret = "";
      for (int i = 0; i < table.Columns.Count; i++)
        if (table.Cells[0, i].GetTextString(FormatOption.IgnoreMtextFormat).Length != 0) // отбросить пустые столбцы
          ret += (ret.Length == 0 ? "" : ";") + table.Cells[0, i].GetTextString(FormatOption.IgnoreMtextFormat);
      return ret;
    }

    /// <summary>
    /// Проверяет, не удалил ли пользователь стандартные стили ячеек таблицы
    /// </summary>
    /// <param name="table"></param>
    /// <param name="tr"></param>
    /// <returns></returns>
    public static bool
    CheckCellStyles(this Table table, Transaction tr)
    {
      if (table is null || tr is null) return false;
      TableStyle tableStyle = tr.GetObject(table.TableStyle, OpenMode.ForRead) as TableStyle;
      if (tableStyle is null) return false;
      bool title = false;
      bool header = false;
      bool data = false;
      foreach (object st in tableStyle.CellStyles)
        if (st is string str)
          if (str == "_TITLE") title = true;
          else if (str == "_HEADER") header = true;
          else if (str == "_DATA") data = true;
      return title && header && data;
    }

    /// <summary>
    /// Ячейка таблицы имеет стиль _TITLE
    /// </summary>
    public static bool
    IsTitle(this Table table, int row, int col)
    {
      if (table is null || table.IsDisposed || table.IsErased) return false;
      if (table.Cells[row, col].Style == "") // byRow|Col
        if (table.Columns[col].Style == "") // byRow
          return table.Rows[row].Style == "_TITLE"; // во всех локализациях стандартные стили названы по английски
        else // byCol
          return table.Columns[col].Style == "_TITLE";
      return table.Cells[row, col].Style == "_TITLE";
    }

    /// <summary>
    /// Ячейка таблицы имеет стиль _HEADER
    /// </summary>
    public static bool
    IsHeader(this Table table, int row, int col)
    {
      if (table is null || table.IsDisposed || table.IsErased) return false;
      if (table.Cells[row, col].Style == "") // byRow|Col
        if (table.Columns[col].Style == "") // byRow
          return table.Rows[row].Style == "_HEADER"; // во всех локализациях стандартные стили по английски
        else // byCol
          return table.Columns[col].Style == "_HEADER";
      return table.Cells[row, col].Style == "_HEADER";
    }

    /// <summary>
    /// Список номеров строк со стилем _TITLE
    /// </summary>
    /// <param name="tb"></param>
    /// <returns></returns>
    public static List<int>
    Parts(this Table tb)
    {
      List<int> ret = new();
      if (tb is null || tb.Rows.Count <= 1 || tb.Columns.Count == 0) return ret;
      for (int i = 0; i < tb.Rows.Count; i++)
        if (tb.Rows[i].Style == "_TITLE" || tb.Cells[i, 0].Style == "_TITLE")
          ret.Add(i);
      return ret;
    }

    /// <summary>
    /// Удалить все строки от начала до row, кроме одной (последней) строки со стилем _HEADER
    /// </summary>
    public static bool
    DeleteRowsUpper(this Table tb, int row)
    {
      if (tb is null || row <= 0 || tb.Rows.Count <= row) return false;
      bool hasHeader = false;
      for (int i = row - 1; i >= 0; i--)
      {
        if ((tb.Rows[i].Style ?? tb.Cells[i, 0].Style) == "_HEADER" && !hasHeader)
        { hasHeader = true; continue; }
        tb.DeleteRows(i, 1);
      }
      return true;
    }

    public static double
    TextWidth(this Table tb, int row, int col, string text = "")
    {
      if (tb is null || tb.IsErased
        || tb.Rows.Count - 1 < row || tb.Columns.Count - 1 < col
        || (text == "" && tb.Cells[row, col].TextString == "")
        || tb.Cells[row, col].TextStyleId is null // бывает у объединенных ячеек null
        || tb.Cells[row, col].TextHeight is null
        || tb.Cells[row, col].TextHeight <= 0) // бывает у объединенных ячеек -1
        return 0;
      using MText temp = new();
      if (text.Contains("%<")) // текст содержит поля
      {
        using Field field = new(text, true);
        try
        {
          field.Evaluate(0, tb.Database);
          text = field.GetStringValue();
        }
        catch { } // игнорировать ошибки вычисления поля
      }
      temp.Contents = (text == "" ? tb.Cells[row, col].TextString : text);
      temp.TextStyleId = tb.Cells[row, col].TextStyleId.Value;
#if !BRICS
      temp.SetFromStyle(); // вызывает eNotImplementedYet в БриксКАД
#endif
      temp.TextHeight = tb.Cells[row, col].TextHeight.Value;
      if (tb.Cells[row, col].Contents.Count > 0)
        temp.Rotation = tb.Cells[row, col].Contents[0].Rotation;
      Extents3d exts = temp.GeometricExtents;
      return exts.MaxPoint.X - exts.MinPoint.X;
    }

    /// <summary>
    /// Копировать все строки из source в конец таблицы target. 
    /// Первая строка source игнорируется, если там заголовки. Количество столбцов должно быть идентично
    /// </summary>
    public static void
    AddRows(this Table target, Table source)
    {
      if (target is null || target.IsErased || source is null
        || source.IsErased || source.Rows.Count == 0 || source.Columns.Count != target.Columns.Count) return;
      int startRow = source.Rows[0].Style == "_HEADER" ? 1 : 0;
      if (startRow == 1 && source.Rows.Count == 1) return;
      int insertTo = target.Rows.Count;
#if BRICS // отсутствует CopyFrom с диапазоном ячеек 
      int insertCount = source.Rows.Count - startRow;
      target.InsertRows(insertTo, target.Rows[target.Rows.Count - 1].Height, insertCount);
      for (int row = startRow; row < source.Rows.Count; row++)
      {
        target.Rows[insertTo].Style = source.Rows[row].Style;
        CellRange sourceRow = CellRange.Create(source, row, 0, row, source.Columns.Count - 1); // вся текущая строка
        CellRange targetRow = CellRange.Create(target, insertTo, 0, insertTo, target.Columns.Count - 1);
        targetRow.UnlockContent();
        // объединение заголовков
        try
        {
          if (sourceRow.IsMerged == true)
          {
            if (targetRow.IsMerged != true) target.MergeCells(targetRow);
          }
          else
          {
            if (targetRow.IsMerged != false) target.UnmergeCells(targetRow);
          }
        }
        catch (Rt.Exception ex)
        { if (ex.ErrorStatus != ErrorStatus.IsWriteProtected) throw ex; }

        for (int col = 0; col < target.Columns.Count; col++)
        {
          // запишем текст в ячейку
          if (!target.FormatLocked(insertTo, col))
            target.Cells[insertTo, col].DataType = source.Cells[row, col].DataType;
          target.Cells[insertTo, col].TextString = ""; // необходимо сбрасывать, чтоб заменить одно Поле другим              
          target.Cells[insertTo, col].TextString = source.Cells[row, col].TextString;

          // пропуск 2ой и далее объединенных ячеек
          if (target.Cells[insertTo, col].IsMerged == true) // IsMerged == true только у первой из объединенных ячеек
          {
            CellRange cr = target.Cells[insertTo, col].GetMergeRange();
            col += cr.RightColumn - cr.LeftColumn;
          }
        }

        // сжатие высоты строк
        if (target.Rows[insertTo].Height > target.Rows[insertTo].MinimumHeight)
          target.Rows[insertTo].Height = target.Rows[insertTo].MinimumHeight;
      
        insertTo++;
      }
#else
      CellRange sourceRange = CellRange.Create(source, startRow, 0, source.Rows.Count - 1, source.Columns.Count - 1);
      target.InsertRows(insertTo, target.Rows[target.Rows.Count - 1].Height, 1);  // добавим 1 - остальные добавятся сами
      CellRange targetRange = CellRange.Create(target, insertTo, 0, insertTo, 0); // диапазон из 1 ячейки - расширится сам
      target.CopyFrom(source, TableCopyOptions.ExpandOrContractTable | TableCopyOptions.TableCopyRowHeight, sourceRange, targetRange);
      // копирование переносит стиль строк в стиль ячеек. вернем обратно
      for (int i = insertTo; i < target.Rows.Count; i++)
        target.Rows[i].Style = target.Cells[i, 0].Style;
#endif
    }

    /// <summary>
    /// новая пустая таблица
    /// </summary>
    /// <param name="tb"></param>
    /// <returns></returns>
    public static bool
    IsNew(this Table tb)
    {
      if (tb == null) return false;
      return tb.Rows.Count == 0 || tb.Columns.Count == 0
            || (tb.Rows.Count == 1 && tb.Columns.Count == 1 && tb.Cells[0, 0].TextString == "");
    }

  }
}
