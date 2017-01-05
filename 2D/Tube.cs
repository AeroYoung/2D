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
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.GraphicsInterface;

class TubeJig : DrawJig
{
    public DBObjectCollection results = new DBObjectCollection();
    public List<Point3d> ps = new List<Point3d>();
    public Point3d tempPt = new Point3d();
    public bool finish = false;

    public TubeJig(List<Point3d> ps)
    {
        this.ps = ps;
    }

    protected override SamplerStatus Sampler(JigPrompts prompts)
    {
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database acCurDb = doc.Database;
        Editor editor = doc.Editor;
        Matrix3d mt = editor.CurrentUserCoordinateSystem;

        //定义1个点拖拽交互类.
        JigPromptPointOptions optJigPoint = new JigPromptPointOptions(
            "\n选择下一个点,或[完成(F)]");
        optJigPoint.Keywords.Add("F");
        optJigPoint.Keywords.Default = "F";
        optJigPoint.Cursor = CursorType.Crosshair;
        
        //拖拽限制
        optJigPoint.UserInputControls =
            UserInputControls.Accept3dCoordinates
            | UserInputControls.NoZeroResponseAccepted
            | UserInputControls.NoNegativeResponseAccepted;

        //WCS
        optJigPoint.BasePoint = ps[ps.Count - 1].TransformBy(mt);
        optJigPoint.UseBasePoint = true;

        //用 AcquirePoint函数获枏拖拽得到的即时点
        PromptPointResult resJigPoint = prompts.AcquirePoint(optJigPoint);
        tempPt = resJigPoint.Value;
        
        if (resJigPoint.Status == PromptStatus.Cancel)
        {
            return SamplerStatus.Cancel;
        }
        else if (resJigPoint.Status == PromptStatus.Keyword)
        {
            if (resJigPoint.StringResult == "F")
            {
                this.finish = true;
                return SamplerStatus.OK;
            }
            else
            {
                return SamplerStatus.NoChange;
            }
        }
        else if (resJigPoint.Status == PromptStatus.OK)
        {
            return SamplerDraw();
        }
        else
        {
            this.finish = true;
            return SamplerStatus.OK;
        }
    }

    protected override bool WorldDraw(WorldDraw draw)
    {
        foreach (Entity ent in results)
        {
            draw.Geometry.Draw(ent);
        }
        return true;
    }

    /// <summary>
    /// 画图
    /// </summary>
    private SamplerStatus SamplerDraw()
    {
        Tolerance tolerance = new Tolerance(0.005, 0.005);
        int count = ps.Count;

        if (ps[count - 1].to2d().IsEqualTo(tempPt.to2d(), tolerance))
        {
            this.finish = false;
            return SamplerStatus.NoChange;
        }
        // TODO 临时点不能在面域之中

        results.Clear();

        List<Point3d> tempPs = new List<Point3d>(ps.ToArray());
        tempPs.Add(tempPt);
        count = tempPs.Count;

        #region 1. 线段

        //1 中心线
        List<Line> centerLine = new List<Line>();
        for (int i = 0; i < count - 1; i++)
        {
            centerLine.Add(new Line(tempPs[i], tempPs[i + 1]));
        }



        #endregion

        for (int i = 0; i < tempPs.Count; i++)
        {
            Circle circle = new Circle(tempPs[i], new Vector3d(0, 0, 1), 27.5);
            results.Add(circle);
        }
        this.finish = false;
        return SamplerStatus.OK;
    }
}