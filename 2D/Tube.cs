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

    private List<Point3d> ps = new List<Point3d>();

    public Point3d tempPt = new Point3d();

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
        JigPromptPointOptions optJigPoint = new JigPromptPointOptions("选择下一个点");
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
        Point3d tempPt = resJigPoint.Value;

        //拖拽完成
        if (resJigPoint.Status == PromptStatus.Cancel)
            return SamplerStatus.Cancel;
        
        if (ps[ps.Count-1] != tempPt)
        {
            List<Point3d> tempPs = new List<Point3d>(ps.ToArray());
            tempPs.Add(tempPt);
                      
            results.Clear();

            for (int i = 0; i < tempPs.Count; i++)
            {
                Circle circle = new Circle(tempPs[i], new Vector3d(0, 0, 1), 50);
                results.Add(circle);
            }            
        }

        return SamplerStatus.OK;
    }

    protected override bool WorldDraw(WorldDraw draw)
    {
        foreach (Entity ent in results)
        {
            draw.Geometry.Draw(ent);
        }
        return true;
    }
}