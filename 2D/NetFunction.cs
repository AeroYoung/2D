using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Net;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using AutoCAD;//AutoCAD Type Library 取代了AutoCAD.Interop
using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

static class NetFunction
{
    #region 调用Acad内部命令

    [DllImport("accore.dll", EntryPoint = "acedCmd", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.Cdecl)]

    private extern static int acedCmd(IntPtr rbp);

    public static int AcedCmd(this Editor editor, ResultBuffer args)
    {
        if (!App.DocumentManager.IsApplicationContext)
            return acedCmd(args.UnmanagedObject);
        else
            return 0;
    }

    public static void SendCommand(this Editor editor, params string[] args)
    {
        DBObjectCollection results = new DBObjectCollection();
        Document doc = App.DocumentManager.MdiActiveDocument;

        Type AcadDoc = Type.GetTypeFromHandle(Type.GetTypeHandle(doc.GetAcadDocument()));

        try
        {
            AcadDoc.InvokeMember("SendCommand", BindingFlags.InvokeMethod, null,
                        doc.GetAcadDocument(), args);
        }
        catch
        {
            return;
        }
    }

    #endregion
    
    #region 用户输入和选择

    /// <summary>
    /// 类型过滤枚举类
    /// </summary>
    public enum FilterType
    {
        Curve, Dimension, Circle, Line
    }

    public static DBObjectCollection GetSelection(String Message, FilterType[] filter)
    {
        DBObjectCollection results = new DBObjectCollection();
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database dataBase = doc.Database;
        Editor editor = doc.Editor;

        //建立选择过滤器
        int len = filter.Length;
        TypedValue[] filterList = new TypedValue[len + 2];
        filterList[0] = new TypedValue((int)DxfCode.Operator, "<or");
        filterList[len + 1] = new TypedValue((int)DxfCode.Operator, "or>");
        for (int i = 0; i < len; i++)
        {
            filterList[i + 1] = new TypedValue((int)DxfCode.Start, filter[i].ToString());
        }
        SelectionFilter selectFilter = new SelectionFilter(filterList);

        //选择
        PromptSelectionOptions options = new PromptSelectionOptions();
        options.MessageForAdding = Message;
        PromptSelectionResult entities = editor.GetSelection(options, selectFilter);

        if (entities.Status == PromptStatus.OK)
        {
            using (Transaction trans = dataBase.TransactionManager.StartTransaction())
            {
                SelectionSet set = entities.Value;
                foreach (ObjectId id in set.GetObjectIds())
                {
                    Entity entity = (Entity)trans.GetObject(id, OpenMode.ForWrite, true);
                    if (entity == null) continue;
                    results.Add(entity);
                }
                trans.Commit();
            }
        }

        return results;
    }

    public static Point3d Pick(String Message)
    {
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database dataBase = doc.Database;
        Editor editor = doc.Editor;

        PromptPointResult pt = editor.GetPoint(Message);
        if (pt.Status == PromptStatus.OK)
            return pt.Value;
        else
            return Point3d.Origin;
    }

    #endregion

    #region 几何运算

    public static Point2d to2d(this Point3d source)
    {
        return new Point2d(source.X, source.Y);
    }

    /// <summary>
    /// 返回二维几何直线
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static Line2d to2d(this Line source)
    {
        return new Line2d(source.StartPoint.to2d(), source.EndPoint.to2d());
    }
    
    public static CircularArc2d to2d(this Arc source)
    {
        return new CircularArc2d(source.Center.to2d(), source.Radius,
            source.StartAngle, source.EndAngle, new Vector2d(1, 0), true);
    }

    public static CircularArc2d to2dCircle(this Arc source)
    {
        return new CircularArc2d(source.Center.to2d(), source.Radius);
    }

    /// <summary>
    /// 二维线段
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static LineSegment2d segment(this Line source)
    {
        return new LineSegment2d(source.StartPoint.to2d(), source.EndPoint.to2d());
    }

    public static Point3d to3d(this Point2d source)
    {
        return new Point3d(source.X, source.Y,0);
    }

    public static Vector2d GetUnitVector(Point2d p1, Point2d p2)
    {
        Vector2d vec = new Vector2d(p2.X - p1.X, p2.Y - p1.Y);
        double len = vec.X * vec.X + vec.Y * vec.Y;

        if (len < 0.001) return vec;

        return new Vector2d(vec.X / Math.Sqrt(len), vec.Y / Math.Sqrt(len));
    }
    
    #endregion

    #region 图层

    /// <summary>
    /// 设置当前图层，不存在则创建图层
    /// </summary>
    /// <param name="targetLayer"></param>
    public static void SetCurrentLayer(String targetLayer)
    {
        DocumentLock docLock = App.DocumentManager.MdiActiveDocument.LockDocument();

        Document doc = App.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;

        using (Transaction trans = db.TransactionManager.StartTransaction())
        {
            LayerTable layer_table = (LayerTable)trans.GetObject(db.LayerTableId,
                OpenMode.ForWrite);

            LinetypeTable acLinTbl;
            acLinTbl = trans.GetObject(db.LinetypeTableId,
                OpenMode.ForRead) as LinetypeTable;

            // 检查图层是否存在
            if (!layer_table.Has(targetLayer))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = targetLayer;
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 151);
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
            }
            trans.Commit();
            db.Clayer = layer_table[targetLayer];
        }

        docLock.Dispose();
    }

    /// <summary>
    /// 图层内所有对象的id
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
    public static ObjectIdCollection GetIdsAtLayer(string layerName)
    {
        ObjectIdCollection ids = new ObjectIdCollection();

        PromptSelectionResult ProSset = null;
        TypedValue[] filList = new TypedValue[1] {
            new TypedValue((int)DxfCode.LayerName, layerName)
        };
        SelectionFilter sfilter = new SelectionFilter(filList);
        Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        ProSset = ed.SelectAll(sfilter);
        if (ProSset.Status == PromptStatus.OK)
        {

            SelectionSet sst = ProSset.Value;

            ObjectId[] oids = sst.GetObjectIds();

            for (int i = 0; i < oids.Length; i++)
            {

                ids.Add(oids[i]);
            }
        }

        return ids;
    }

    public static ObjectIdCollection GetIdsAtLayer(string layerName, FilterType filter)
    {
        ObjectIdCollection ids = new ObjectIdCollection();

        PromptSelectionResult ProSset = null;
        TypedValue[] filList = new TypedValue[2] {
            new TypedValue((int)DxfCode.LayerName, layerName),
            new TypedValue((int)DxfCode.Start, filter.ToString())
        };
        SelectionFilter sfilter = new SelectionFilter(filList);
        Editor ed = App.DocumentManager.MdiActiveDocument.Editor;
        ProSset = ed.SelectAll(sfilter);
        if (ProSset.Status == PromptStatus.OK)
        {

            SelectionSet sst = ProSset.Value;

            ObjectId[] oids = sst.GetObjectIds();

            for (int i = 0; i < oids.Length; i++)
            {

                ids.Add(oids[i]);
            }
        }

        return ids;
    }

    #endregion

    #region 倒角

    /// <summary>
    /// 倒度直角
    /// </summary>
    /// <param name="line1"></param>
    /// <param name="line2"></param>
    /// <param name="value"></param>
    public static Line Chamfer(Line line1, Line line2, double value)
    {
        Tolerance tolerance = new Tolerance(0.001, 0.001);
        double scale1 = 5 / line1.Length;
        double scale2 = 5 / line2.Length;

        LineSegment2d l1 = line1.segment();
        LineSegment2d l2 = line2.segment();
        Point2d[] points = l1.IntersectWith(l2);

        if (points == null) return null;
        if (scale1 >= 1 || scale2 >= 1) return null;

        Point3d newP1, newP2;
        if (line1.StartPoint.to2d().IsEqualTo(points[0], tolerance))
        {
            line1.StartPoint = line1.StartPoint + scale1 * line1.Delta;
            newP1 = line1.StartPoint;
        }
        else
        {
            line1.EndPoint = line1.EndPoint - scale1 * line1.Delta;
            newP1 = line1.EndPoint;
        }

        if (line2.StartPoint.to2d().IsEqualTo(points[0], tolerance))
        {
            line2.StartPoint = line2.StartPoint + scale2 * line2.Delta;
            newP2 = line2.StartPoint;
        }
        else
        {
            line2.EndPoint = line2.EndPoint - scale2 * line2.Delta;
            newP2 = line2.EndPoint;
        }

        return new Line(newP1, newP2);
    }

    /// <summary>
    /// 2直线垂直才倒角
    /// </summary>
    /// <param name="line1"></param>
    /// <param name="line2"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Line Chamfer45(Line line1, Line line2, double value)
    {
        Tolerance tolerance = new Tolerance(0.001, 0.001);
        double scale1 = 5 / line1.Length;
        double scale2 = 5 / line2.Length;

        LineSegment2d l1 = line1.segment();
        LineSegment2d l2 = line2.segment();
        Point2d[] points = l1.IntersectWith(l2, tolerance);

        if (points == null) return null;
        if (scale1 >= 1 || scale2 >= 1) return null;
        if (!l1.IsPerpendicularTo(l2, tolerance)) return null;

        Point3d newP1, newP2;
        if (line1.StartPoint.to2d().IsEqualTo(points[0], tolerance))
        {
            line1.StartPoint = line1.StartPoint + scale1 * line1.Delta;
            newP1 = line1.StartPoint;
        }
        else
        {
            line1.EndPoint = line1.EndPoint - scale1 * line1.Delta;
            newP1 = line1.EndPoint;
        }

        if (line2.StartPoint.to2d().IsEqualTo(points[0], tolerance))
        {
            line2.StartPoint = line2.StartPoint + scale2 * line2.Delta;
            newP2 = line2.StartPoint;
        }
        else
        {
            line2.EndPoint = line2.EndPoint - scale2 * line2.Delta;
            newP2 = line2.EndPoint;
        }

        return new Line(newP1, newP2);
    }

    /// <summary>
    /// 直线倒圆角
    /// </summary>
    /// <param name="line1"></param>
    /// <param name="line2"></param>
    /// <param name="R"></param>
    public static Arc Fillet(Line line1, Line line2, double R)
    {
        if (line1 == null || line2 == null) return null;
        if (line1.Length < R || line2.Length < R) return null;

        Arc arc = null;
        Tolerance tolerance = new Tolerance(0.001, 0.001);

        LineSegment2d l1 = line1.segment();
        LineSegment2d l2 = line2.segment();
        Point2d[] points = l1.IntersectWith(l2,tolerance);
        
        if (points == null) return null;        

        #region 1. 计算圆心坐标

        Point3d pArc3d = new Point3d();
        Point3d pInter3d = new Point3d(points[0].X, points[0].Y,0);
        Vector2d vec1, vec2;

        if (line1.EndPoint.to2d().IsEqualTo(points[0], tolerance))
        {
            vec1 = GetUnitVector(points[0], line1.StartPoint.to2d());
        }
        else
        {
            vec1 = GetUnitVector(points[0], line1.EndPoint.to2d());
        }
        if (line2.EndPoint.to2d().IsEqualTo(points[0], tolerance))
        {
            vec2 = GetUnitVector(points[0], line2.StartPoint.to2d());
        }
        else
        {
            vec2 = GetUnitVector(points[0], line2.EndPoint.to2d());
        }

        Vector2d vec = vec1 + vec2;
        Vector3d vec3d = new Vector3d(vec.X,vec.Y,0);

        double halfAngle = vec1.GetAngleTo(vec2) / 2;//0~pi
        pArc3d = pInter3d + R / Math.Sin(halfAngle) * vec3d.GetNormal();

        #endregion

        #region 2.切点

        Point3d pCut1, pCut2 = new Point3d();

        CircularArc2d circle = new CircularArc2d(pArc3d.to2d(), R);
        Point2d[] pointsC1 = circle.IntersectWith(l1, tolerance);
        Point2d[] pointsC2 = circle.IntersectWith(l2, tolerance);
        if (pointsC1 == null || pointsC2 == null) return arc;

        pCut1 = new Point3d(pointsC1[0].X, pointsC1[0].Y, 0);
        pCut2 = new Point3d(pointsC2[0].X, pointsC2[0].Y, 0);

        #endregion

        #region 3.圆弧

        //CircularArc2d arc2d = new CircularArc2d(pCut1.to2d(),pArc3d.to2d(), pCut2.to2d());
        Vector2d vCut1 = GetUnitVector(pArc3d.to2d(), pointsC1[0]);
        Vector2d vCut2 = GetUnitVector(pArc3d.to2d(), pointsC2[0]);

        arc = new Arc(pArc3d, R, vCut1.Angle, vCut2.Angle);
        if (arc.Length > Math.PI * R)
            arc = new Arc(pArc3d, R, vCut2.Angle, vCut1.Angle);

        #endregion

        #region 4. 修改直线终点

        if (!l1.IsOn(pCut1.to2d(), tolerance) || !l2.IsOn(pCut2.to2d(), tolerance))
            return null;

        if (line1.StartPoint.to2d().IsEqualTo(pInter3d.to2d(), tolerance))
            line1.StartPoint = pCut1;
        else
            line1.EndPoint = pCut1;

        if (line2.StartPoint.to2d().IsEqualTo(pInter3d.to2d(), tolerance))
            line2.StartPoint = pCut2;
        else
            line2.EndPoint = pCut2;

        #endregion

        return arc;
    }

    #endregion

    #region 编辑对象
    
    /// <summary>
    /// 偏移直线
    /// </summary>
    /// <param name="targetLayer">目标图层</param>
    /// <param name="source">偏移对象</param>
    /// <param name="value">偏移值</param>
    public static DBObjectCollection OffsetLines(String targetLayer, List<Line> source, double value)
    {
        DBObjectCollection results = new DBObjectCollection();
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database acCurDb = doc.Database;
        Editor editor = doc.Editor;

        using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
        {
            // 以读模式打开 Block 表
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                OpenMode.ForRead) as BlockTable;
            // 以写模式打开块表记录模型空间
            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite) as BlockTableRecord;

            foreach (Line line in source)
            {
                DBObjectCollection objs = line.GetOffsetCurves(value);
                foreach (Entity entity in objs)
                {
                    results.Add(entity);
                    // 添加每个对象                
                    acBlkTblRec.AppendEntity(entity);
                    acTrans.AddNewlyCreatedDBObject(entity, true);
                    Line target = (Line)acTrans.GetObject(entity.ObjectId,
                        OpenMode.ForWrite);

                    target.Layer = targetLayer;
                    target.Linetype = "ByLayer";
                    //target.Color = Color.FromDictionaryName("ByLayer");
                }
            }
            acTrans.Commit();
        }
        return results;
    }
    
    #endregion

    /// <summary>
    /// 所有曲线的端点
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static List<Point3d> GetEndPoints(DBObjectCollection source)
    {
        List<Point3d> results = new List<Point3d>();

        foreach (Entity entity in source)
        {
            Line line = (Line)entity;
            if(line!=null)
            {
                results.Add(line.StartPoint);
                results.Add(line.EndPoint);
            }
            
        }
        return results;
    }
    
    /// <summary>
    /// 将实体加入块表记录模型空间
    /// </summary>
    /// <param name="acDBObjColl"></param>
    public static void Add2BlockModelSpace(this DBObjectCollection acDBObjColl)
    {
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database acCurDb = doc.Database;
        Editor editor = doc.Editor;

        using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
        {
            // 以读模式打开 Block 表
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                OpenMode.ForRead) as BlockTable;
            // 以写模式打开块表记录模型空间
            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite) as BlockTableRecord;

            foreach (Entity entity in acDBObjColl)
            {
                acBlkTblRec.AppendEntity(entity);
                acTrans.AddNewlyCreatedDBObject(entity, true);
            }

            acTrans.Commit();
        }
    }
    
    /// <summary>
    /// 合并2个Collection
    /// </summary>
    /// <param name="objs1"></param>
    /// <param name="objs2"></param>
    /// <returns></returns>
    public static DBObjectCollection CombineCollection(DBObjectCollection objs1, DBObjectCollection objs2)
    {
        foreach (Entity entity in objs2)
        {
            if (entity == null) continue;
            objs1.Add(entity);
        }
        return objs1;
    }

    #region 块与导入

    /// <summary>
    /// 导入块定义
    /// </summary>
    /// <param name="targetDb"></param>
    /// <param name="filePath"></param>
    public static void ImportBlockFromDwg(this Database targetDb, string filePath)
    {
        Database sourceDb = new Database(false,true);
        ObjectIdCollection blockIds = new ObjectIdCollection();
        try
        {
            sourceDb.ReadDwgFile(filePath, FileShare.Read, true, null);            

            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = sourceDb.TransactionManager;
            using (Transaction myT = tm.StartTransaction())
            {
                BlockTable bt = (BlockTable)tm.GetObject(sourceDb.BlockTableId, 
                    OpenMode.ForRead, false);

                BlockTableRecord btr;
                btr = myT.GetObject(bt[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId id in btr)
                {
                    //获得块参照
                    if (id.ObjectClass.DxfName != "INSERT") continue;
                    BlockReference block_reference;
                    block_reference = myT.GetObject(id,OpenMode.ForRead) as BlockReference;

                    blockIds.Add(id);
                }

                btr.Dispose();
                bt.Dispose();
            }

            IdMapping mapping = new IdMapping();
            sourceDb.WblockCloneObjects(blockIds, targetDb.BlockTableId, mapping, 
                DuplicateRecordCloning.Replace, false);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception ex)
        {
            App.ShowAlertDialog("导入文件" + filePath + "出错:\n" + ex.Message);
        }

        sourceDb.Dispose();
    }

    /// <summary>
    /// 插入块参照 （已加入 && 未提交）
    /// </summary>
    /// <param name="spaceId"></param>
    /// <param name="layer"></param>
    /// <param name="blockName"></param>
    /// <param name="position"></param>
    /// <param name="angle"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static ObjectId InsertBlockReference(this ObjectId spaceId, string layer,
        string blockName, Point3d position, double angle, Scale3d scale)
    {
        ObjectId blockRefId = new ObjectId();
        Database db = spaceId.Database;

        BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
        if (!bt.Has(blockName)) return ObjectId.Null;

        BlockTableRecord space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
        BlockReference br = new BlockReference(position,bt[blockName]);
        br.ScaleFactors = scale;
        br.Layer = layer;
        br.Rotation = angle;

        blockRefId = space.AppendEntity(br);
        db.TransactionManager.AddNewlyCreatedDBObject(br, true);

        space.DowngradeOpen();
        return blockRefId;
    }

    /// <summary>
    /// 插入块参照，然后炸开（未加入 && 未提交）
    /// </summary>
    /// <param name="spaceId"></param>
    /// <param name="layer"></param>
    /// <param name="blockName"></param>
    /// <param name="position"></param>
    /// <param name="angle"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static DBObjectCollection InsertBlockReferenceThenExplore(this ObjectId spaceId, string layer,
        string blockName, Point3d position, double angle, Scale3d scale)
    {
        DBObjectCollection objs = new DBObjectCollection();
        Database db = spaceId.Database;

        using (Transaction trans = db.TransactionManager.StartTransaction())
        {
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blockName)) return objs;

            BlockTableRecord space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            BlockReference br = new BlockReference(position, bt[blockName]);
            br.ScaleFactors = scale;
            br.Layer = layer;
            br.Rotation = angle;

            br.Explode(objs);
        }
        
        return objs;
    }

    #endregion
}

class ManifoldBuilder
{
    private Manifold manifold;

    private List<Line> runner = new List<Line>();

    public List<Line> Runner { get { return runner; } }

    public ManifoldBuilder(Manifold value)
    {
        this.manifold = value;
    }

    public ManifoldBuilder() { }

    public void SetRunner(DBObjectCollection objs)
    {
        foreach (Entity entity in objs)
        {
            if (entity.ObjectId.ObjectClass.DxfName.ToString() != "LINE") continue;
            Line line = (Line)entity;
            runner.Add(line);
        }
    }
    
    public void SimpleManifoldContour2()
    {
        if (runner.Count == 0) return;

        DBObjectCollection results = new DBObjectCollection();
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database acCurDb = doc.Database;
        Editor editor = doc.Editor;
        String targetLayer = "1manifold";
        NetFunction.SetCurrentLayer(targetLayer);

        DBObjectCollection regionCollection = new DBObjectCollection();

        #region 创建临时边和临时面
        for(int k=0;k<runner.Count;k++)
        {
            //延伸直线
            Line line = new Line(runner[k].StartPoint, runner[k].EndPoint);
            int start = 0;
            int end = 0;
            for (int j = 0; j < runner.Count; j++)
            {
                if (j == k) continue;
                
                if (Math.Abs(runner[j].to2d().GetDistanceTo(line.StartPoint.to2d()))<0.001)
                {
                    start = 1;
                }
                if (Math.Abs(runner[j].to2d().GetDistanceTo(line.EndPoint.to2d())) < 0.001)
                {
                    end = 1;
                }
            }
            if (start == 0) line.StartPoint -= 55 / line.Length * line.Delta;
            if (end == 0) line.EndPoint += 55 / line.Length * line.Delta;
            
            //长边
            DBObjectCollection objs1 = line.GetOffsetCurves(35);
            DBObjectCollection objs2 = line.GetOffsetCurves(-35);

            //短边
            List<Point3d> points1 = NetFunction.GetEndPoints(objs1);
            List<Point3d> points2 = NetFunction.GetEndPoints(objs2);

            DBObjectCollection edges = NetFunction.CombineCollection(objs1, objs2);

            foreach (Point3d p in points1)
            {
                for (int i = 0; i < points2.Count; i++)
                {
                    if (Math.Round(p.DistanceTo(points2[i]), 2) != 70.00) continue;
                    Line acLine = new Line(p, points2[i]);
                    points2.RemoveAt(i);
                    edges.Add(acLine);
                }
            }

            //倒直角
            DBObjectCollection chamfers = new DBObjectCollection();
            for (int i = 0; i < edges.Count; i++)
            {
                for (int j = i + 1; j < edges.Count; j++)
                {
                    Line chamfer = NetFunction.Chamfer45((Line)(edges[i] as Entity),
                        (Line)(edges[j] as Entity), 5);
                    if (chamfer != null) chamfers.Add(chamfer);
                }
            }
            edges = NetFunction.CombineCollection(edges, chamfers);

            //面域
            DBObjectCollection myRegionColl = Region.CreateFromCurves(edges);
            Region acRegion = myRegionColl[0] as Region;
            regionCollection.Add(acRegion);
        }
        #endregion

        #region 完整面域
        Region region = regionCollection[0] as Region;
        for (int i = 1; i < regionCollection.Count; i++)
        {
            region.BooleanOperation(BooleanOperationType.BoolUnite,
                regionCollection[i] as Region);
        }

        #endregion

        #region 分解面域和倒圆角
        DBObjectCollection acDBObjColl = new DBObjectCollection();
        region.Explode(acDBObjColl);
        
        DBObjectCollection fillets = new DBObjectCollection();
        for (int i = 0; i < acDBObjColl.Count; i++)
        {
            for (int j = i + 1; j < acDBObjColl.Count; j++)
            {
                Arc fillet = NetFunction.Fillet((Line)(acDBObjColl[i] as Entity),
                    (Line)(acDBObjColl[j] as Entity), 15);
                if (fillets != null) fillets.Add(fillet);
            }
        }
        acDBObjColl = NetFunction.CombineCollection(acDBObjColl, fillets);
        
        #endregion

        acDBObjColl.Add2BlockModelSpace();
    }
}

class Manifold
{
    private double width = 70;

    public double Width { get { return width; } set { width = value; } }

    public Manifold()
    { }
}
