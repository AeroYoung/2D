using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.Net;
using Autodesk.Windows;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using AutoCAD;//AutoCAD Type Library 取代了AutoCAD.Interop
using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using Color = Autodesk.AutoCAD.Colors.Color;
using App = Autodesk.AutoCAD.ApplicationServices.Application;
using MenuItem = Autodesk.AutoCAD.Windows.MenuItem;
using RegistryKey = Autodesk.AutoCAD.Runtime.RegistryKey;
using Registry = Autodesk.AutoCAD.Runtime.Registry;
using RibbonControl = Autodesk.Windows.RibbonControl;
using RibbonTab = Autodesk.Windows.RibbonTab;
using RibbonPanelSource = Autodesk.Windows.RibbonPanelSource;
using RibbonButton = Autodesk.Windows.RibbonButton;

[assembly: CommandClass(typeof(Main))]
public class Main : IExtensionApplication
{
    #region 公共变量
    ContextMenuExtension context_menu = new ContextMenuExtension();
    const string APP_NAME = "ugims";
    const int VERSION_NUMBER = 16123100;
    const string RIBBONID = "MYRIBBON";
    #endregion

    #region 初始化和卸载

    public void Initialize()
    {
        //顶部菜单
        InitPopupMenu();
        //右键快捷菜单
        InitContextMenu();
        //自动注册
        RegApp();
        //初始化图层
        //Init();
        //InitRibbon
        ComponentManager.ItemInitialized +=
            new EventHandler<RibbonItemEventArgs>(InitRibbon);
    }

    public void Terminate()
    {
        throw new NotImplementedException();
    }

    private void InitRibbon(object sender, RibbonItemEventArgs e)
    {
        if (ComponentManager.Ribbon == null) return;
        RibbonControl ribCntrl = ComponentManager.Ribbon;

        //add the tab
        RibbonTab ribTab = new RibbonTab();
        ribTab.Title = "热流道快速设计";
        ribTab.Id = RIBBONID;
        ribCntrl.Tabs.Add(ribTab);

        #region 初始化Pannel

        RibbonPanelSource pSource0 = new RibbonPanelSource();
        pSource0.Name = "Panel0";
        pSource0.Title = "初始化";

        //create the panel
        RibbonPanel ribPanel0 = new RibbonPanel();
        ribPanel0.Source = pSource0;
        ribTab.Panels.Add(ribPanel0);

        #region button 图层初始化
        RibbonButton btnInit = new RibbonButton();
        btnInit.Text = "创建图层";
        btnInit.ShowText = true;
        btnInit.ToolTip = "创建快速设计所用的所有图层";
        btnInit.ShowToolTipOnDisabled = true;
        btnInit.IsToolTipEnabled = true;
        btnInit.Size = RibbonItemSize.Large;
        btnInit.LargeImage = LoadImage(_2D.Resource1.init_32px);
        btnInit.Image = LoadImage(_2D.Resource1.init_16px);

        btnInit.Orientation = System.Windows.Controls.Orientation.Vertical;
        btnInit.CommandParameter = "Init ";//后面必须有空格
        btnInit.CommandHandler = new AdskCommandHandler();

        pSource0.Items.Add(btnInit);
        #endregion

        #region button dll路径
        RibbonButton btnDllPath = new RibbonButton();
        btnDllPath.Text = "Dll";
        btnDllPath.ShowText = true;
        btnDllPath.ToolTip = "查看热流道快速设计插件路径";
        btnDllPath.ShowToolTipOnDisabled = true;
        btnDllPath.IsToolTipEnabled = true;
        btnDllPath.Size = RibbonItemSize.Standard;
        btnDllPath.LargeImage = LoadImage(_2D.Resource1.DLL_32px);
        btnDllPath.Image = LoadImage(_2D.Resource1.DLL_16px);

        btnDllPath.Orientation = System.Windows.Controls.Orientation.Horizontal;
        btnDllPath.CommandParameter = "ShowDllPath ";//后面必须有空格
        btnDllPath.CommandHandler = new AdskCommandHandler();

        pSource0.Items.Add(btnDllPath);
        #endregion

        #region button 命令列表
        RibbonButton btnCtrl = new RibbonButton();
        btnCtrl.Text = "命令";
        btnCtrl.ShowText = true;
        btnCtrl.ToolTip = "命令列表";
        btnCtrl.ShowToolTipOnDisabled = true;
        btnCtrl.IsToolTipEnabled = true;
        btnCtrl.Size = RibbonItemSize.Standard;
        btnCtrl.LargeImage = LoadImage(_2D.Resource1.Ctrl_32px);
        btnCtrl.Image = LoadImage(_2D.Resource1.Ctrl_16px);
        btnCtrl.Orientation = System.Windows.Controls.Orientation.Horizontal;
        btnCtrl.GroupLocation = Autodesk.Private.Windows.RibbonItemGroupLocation.Middle;

        btnCtrl.CommandParameter = "ShortcutCommand ";//后面必须有空格
        btnCtrl.CommandHandler = new AdskCommandHandler();

        pSource0.Items.Add(btnCtrl);
        #endregion

        #endregion

        #region 分流板轮廓Panel

        RibbonPanelSource ribPanelSource = new RibbonPanelSource();
        ribPanelSource.Name = "Panel1";
        ribPanelSource.Title = "分流板轮廓";

        //create the panel
        RibbonPanel ribPanel = new RibbonPanel();
        ribPanel.Source = ribPanelSource;
        ribTab.Panels.Add(ribPanel);

        #region button 简单轮廓
        RibbonButton btnCreateManifold = new RibbonButton();
        btnCreateManifold.Text = "简单轮廓";
        btnCreateManifold.ShowText = true;
        btnCreateManifold.ToolTip = "分流板轮廓初步生成";
        btnCreateManifold.ShowToolTipOnDisabled = true;
        btnCreateManifold.IsToolTipEnabled = true;
        btnCreateManifold.Size = RibbonItemSize.Large;
        btnCreateManifold.LargeImage = LoadImage(_2D.Resource1.MergeVertical_32px);
        btnCreateManifold.Image = LoadImage(_2D.Resource1.MergeVertical_16px);

        btnCreateManifold.Orientation = System.Windows.Controls.Orientation.Vertical;
        btnCreateManifold.CommandParameter = "CreateManifold ";//后面必须有空格
        btnCreateManifold.CommandHandler = new AdskCommandHandler();

        ribPanelSource.Items.Add(btnCreateManifold);
        #endregion

        #region button 热嘴叉耳
        RibbonButton btnNozzleEar = new RibbonButton();
        btnNozzleEar.Text = "热嘴叉耳";
        btnNozzleEar.ShowText = true;
        btnNozzleEar.ToolTip = "热嘴两侧的叉耳";
        btnNozzleEar.ShowToolTipOnDisabled = true;
        btnNozzleEar.IsToolTipEnabled = true;
        btnNozzleEar.Size = RibbonItemSize.Large;
        btnNozzleEar.LargeImage = LoadImage(_2D.Resource1.ear_32px);
        btnNozzleEar.Image = LoadImage(_2D.Resource1.ear_16px);

        btnNozzleEar.Orientation = System.Windows.Controls.Orientation.Vertical;
        btnNozzleEar.CommandParameter = "NozzleEar ";//后面必须有空格
        btnNozzleEar.CommandHandler = new AdskCommandHandler();

        ribPanelSource.Items.Add(btnNozzleEar);
        #endregion

        #endregion

        #region 发热管Pannel

        RibbonPanelSource pSource2 = new RibbonPanelSource();
        pSource2.Name = "Panel2";
        pSource2.Title = "发热管";

        //create the panel
        RibbonPanel ribPanel2 = new RibbonPanel();
        ribPanel2.Source = pSource2;
        ribTab.Panels.Add(ribPanel2);

        #region button 发热管槽
        RibbonButton btnSimpleTube = new RibbonButton();
        btnSimpleTube.Text = "发热管槽";
        btnSimpleTube.ShowText = true;
        btnSimpleTube.ToolTip = "发热管槽初步轮廓";
        btnSimpleTube.ShowToolTipOnDisabled = true;
        btnSimpleTube.IsToolTipEnabled = true;
        btnSimpleTube.Size = RibbonItemSize.Large;
        btnSimpleTube.LargeImage = LoadImage(_2D.Resource1.Return_32px);
        btnSimpleTube.Image = LoadImage(_2D.Resource1.Return_16px);

        btnSimpleTube.Orientation = System.Windows.Controls.Orientation.Vertical;
        btnSimpleTube.CommandParameter = "SimpleTube ";//后面必须有空格
        btnSimpleTube.CommandHandler = new AdskCommandHandler();

        pSource2.Items.Add(btnSimpleTube);
        #endregion        

        #endregion

        #region 发热管Pannel

        RibbonPanelSource pSource3 = new RibbonPanelSource();
        pSource3.Name = "Panel3";
        pSource3.Title = "3D设计";

        //create the panel
        RibbonPanel ribPanel3 = new RibbonPanel();
        ribPanel3.Source = pSource3;
        ribTab.Panels.Add(ribPanel3);

        #region button SQL
        RibbonButton btnSql = new RibbonButton();
        btnSql.Text = "保存数据";
        btnSql.ShowText = true;
        btnSql.ToolTip = "保存图块数据到UG";
        btnSql.ShowToolTipOnDisabled = true;
        btnSql.IsToolTipEnabled = true;
        btnSql.Size = RibbonItemSize.Large;
        btnSql.LargeImage = LoadImage(_2D.Resource1.Screensharing_32px);
        btnSql.Image = LoadImage(_2D.Resource1.Screensharing_16px);

        btnSql.Orientation = System.Windows.Controls.Orientation.Vertical;
        btnSql.CommandParameter = "Sql ";//后面必须有空格
        btnSql.CommandHandler = new AdskCommandHandler();

        pSource3.Items.Add(btnSql);
        #endregion        

        #endregion

        //set as active tab
        ribTab.IsActive = true;

        //Must remove the event handler
        ComponentManager.ItemInitialized -=
            new EventHandler<RibbonItemEventArgs>(InitRibbon);
    }

    private System.Windows.Media.Imaging.BitmapImage LoadImage(Bitmap bitmap)
    {
        MemoryStream ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        System.Windows.Media.Imaging.BitmapImage bitImage = new System.Windows.Media.Imaging.BitmapImage();
        bitImage.BeginInit();
        bitImage.StreamSource = ms;
        bitImage.EndInit();
        return bitImage;
    }

    public void InitPopupMenu()
    {
        AcadApplication acadApp = (AcadApplication)App.AcadApplication;
        AcadPopupMenu popMenu = acadApp.MenuGroups.Item(0).Menus.Add("热流道快速设计");

        AcadPopupMenuItem menuItem1 = popMenu.AddMenuItem(popMenu.Count + 1, "保存图块信息", "ShowDllPath\n");
        AcadPopupMenuItem menuItem2 = popMenu.AddMenuItem(popMenu.Count + 1, "初始化图层", "Init\n");
        popMenu.AddSeparator(popMenu.Count + 1);
        AcadPopupMenuItem menuItem3 = popMenu.AddMenuItem(popMenu.Count + 1, "功能和快捷命令列表", "ShowDllPath\n");
        AcadPopupMenuItem menuItem4 = popMenu.AddMenuItem(popMenu.Count + 1, "查看DLL路径", "ShowDllPath\n");

        popMenu.InsertInMenuBar(acadApp.MenuBar.Count + 1);
    }

    public void InitContextMenu()
    {
        context_menu.Title = "热流道快速设计";

        MenuItem menu_item1 = new MenuItem("保存图块信息");
        MenuItem menu_item2 = new MenuItem("初始化图层");
        MenuItem menu_item3 = new MenuItem("功能和快捷命令列表");
        MenuItem menu_item4 = new MenuItem("查看DLL路径");

        menu_item1.Click += new EventHandler(ItemClick_sql);
        menu_item2.Click += new EventHandler(ItemClick_init);
        menu_item3.Click += new EventHandler(ItemClick_info);
        menu_item4.Click += new EventHandler(ItemClick_path);

        context_menu.MenuItems.Add(menu_item1);
        context_menu.MenuItems.Add(menu_item2);
        context_menu.MenuItems.Add(menu_item3);
        context_menu.MenuItems.Add(menu_item4);

        App.AddDefaultContextMenuExtension(context_menu);
    }

    #endregion

    #region 右键菜单事件
    void ItemClick_sql(object sender, EventArgs e)
    {
        Sql();
    }

    void ItemClick_init(object sender, EventArgs e)
    {
        Init();
    }

    void ItemClick_info(object sender, EventArgs e)
    {
        App.ShowAlertDialog("保存图块数据：Sql\n生成所需图层：init\n注册：RegApp\n卸载：unRegApp");
    }

    void ItemClick_path(object sender, EventArgs e)
    {
        ShowDllPath();
    }
    #endregion

    #region 注册dll
    [CommandMethod("RegApp")]
    public void RegApp()
    {
        string root_key = HostApplicationServices.Current.UserRegistryProductRootKey;

        RegistryKey reg_cad_key = Registry.CurrentUser.OpenSubKey(root_key);
        RegistryKey reg_app_key = reg_cad_key.OpenSubKey("Applications", true);

        //检查"ugims" 键的版本
        string str_version = "0";
        int current_version = 0;
        string[] sub_keys = reg_app_key.GetSubKeyNames();
        foreach (string sub_key in sub_keys)
        {
            if (!sub_key.Equals(APP_NAME)) continue;

            RegistryKey reg_old_key = reg_app_key.OpenSubKey(sub_key, true);
            foreach (string value_name in reg_old_key.GetValueNames())
            {
                if (!value_name.Equals("VERSION")) continue;
                str_version = reg_old_key.GetValue(value_name).ToString();
            }
            reg_old_key.Close();
            if (str_version.Trim() == "") str_version = "0";
        }

        //版本判断，是否删除原来的
        current_version = int.Parse(str_version);
        if (current_version == VERSION_NUMBER) return;

        if (current_version != 0)
            reg_app_key.DeleteSubKeyTree(APP_NAME);

        //获取本模块的位置（注册本程序自己）
        string dll_path = Assembly.GetExecutingAssembly().Location;

        // 注册应用程序
        RegistryKey reg_addin_key = reg_app_key.CreateSubKey(APP_NAME);
        reg_addin_key.SetValue("DESCRIPTION", APP_NAME, RegistryValueKind.String);
        reg_addin_key.SetValue("LOADCTRLS", 14, RegistryValueKind.DWord);
        reg_addin_key.SetValue("LOADER", dll_path, RegistryValueKind.String);
        reg_addin_key.SetValue("MANAGED", 1, RegistryValueKind.DWord);
        reg_addin_key.SetValue("VERSION", VERSION_NUMBER, RegistryValueKind.String);

        reg_app_key.Close();
        reg_cad_key.Close();
        App.ShowAlertDialog("IMS插件注册成功！\n" + "版本号：" + VERSION_NUMBER);
    }

    [CommandMethod("unRegApp")]
    public void unRegApp()
    {
        string root_key = HostApplicationServices.Current.UserRegistryProductRootKey;
        RegistryKey reg_cad_key = Registry.CurrentUser.OpenSubKey(root_key);
        RegistryKey reg_app_key = reg_cad_key.OpenSubKey("Applications", true);

        reg_app_key.DeleteSubKeyTree(APP_NAME);
        reg_app_key.Close();

        App.RemoveDefaultContextMenuExtension(context_menu);

        App.ShowAlertDialog("IMS插件卸载成功！");
    }
    #endregion

    #region 注册命令

    [CommandMethod("Init")]
    public void Init()
    {
        DocumentLock docLock = App.DocumentManager.MdiActiveDocument.LockDocument();

        Document doc = App.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;
        Editor editor = doc.Editor;

        using (Transaction trans = db.TransactionManager.StartTransaction())
        {
            LayerTable layer_table = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite);
            // 以读模式打开图层表
            LinetypeTable acLinTbl;
            acLinTbl = trans.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

            //2——检查图层是否存在
            #region
            if (!layer_table.Has("1manifold"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "1manifold";
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 151);
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成1manifold图层");
            }
            if (!layer_table.Has("2runner"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "2runner";
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成2runner图层");
            }
            if (!layer_table.Has("1submanifold"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "1submanifold";
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 71);
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成1submanifold图层");
            }
            if (!layer_table.Has("7gasline"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "7gasline";
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 6);
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成7gasline图层");
            }
            if (!layer_table.Has("3heater"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "3heater";
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 40);
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成3heater图层");
            }
            if (!layer_table.Has("wireframe"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "wireframe";
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 61);
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成wireframe图层");
            }
            if (!layer_table.Has("ear"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "ear";
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 96);
                layer_table.Add(ltr);
                if (acLinTbl.Has("CONTINUOUS") == true)
                    ltr.LinetypeObjectId = acLinTbl["CONTINUOUS"];
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成ear图层");
            }
            if (!layer_table.Has("0"))
            {
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = "0";
                layer_table.Add(ltr);
                trans.AddNewlyCreatedDBObject(ltr, true);
                editor.WriteMessage("\n:生成0图层");
            }
            #endregion

            trans.Commit();
        }
        editor.WriteMessage("\n:图层初始化成功\n");
        docLock.Dispose();
    }

    [CommandMethod("Sql")]
    public void Sql()
    {
        try
        {
            DocumentLock docLock = App.DocumentManager.MdiActiveDocument.LockDocument();
            int error_nums = 0;
            int warning_nums = 0;
            List<string> error_messages = new List<string>();
            List<string> warning_messages = new List<string>();
            //List<NozzleIndex> nozzle_index = new List<NozzleIndex>();
            //0——预操作 初始化
            error_nums += CheckLayer(ref error_messages, ref warning_nums, ref warning_messages);
            error_nums += CheckBlock(ref error_messages, ref warning_nums, ref warning_messages);

            //1——获得当前文档和文件名 
            Document acDoc = App.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            DataControl sql = new DataControl();
            String host_name = Dns.GetHostName();
            String file_name = acDoc.Name;
            file_name = file_name.Substring(file_name.LastIndexOf('\\') + 1);
            file_name = file_name.Substring(0, file_name.LastIndexOf('.'));

            //2——重复判断
            String strSql = "select count(*) as Expr1 from block_table where host_name='" + host_name + "' and file_name='" + file_name + "'";
            int count = sql.GetExpr1AsInt(strSql);
            if (count > 1)
            {
                if (MessageBox.Show("已经存在相同记录，是否覆盖?", "提示", MessageBoxButtons.OKCancel) != DialogResult.OK)
                    return;
            }
            strSql = "delete from block_table where host_name='" + host_name + "' and file_name='" + file_name + "'";
            sql.dataExec(strSql);

            //3——错误判断
            if (error_nums > 0)
            {
                string err = "保存失败！点击确定按钮退出插件\n";
                for (int i = 0; i < error_messages.Count; i++) err += error_messages[i] + "\n";
                MessageBox.Show(err, "错误！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //4——保存图块数据
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                //2.1——以读模式打开Block表
                BlockTable block_tables;
                block_tables = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;


                //2.2——以读模式打开模型空间 BlockTable.ModelSpace
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(block_tables[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForRead) as BlockTableRecord;

                int index = 0;
                //2.3——遍历模型空间ModelSpace
                foreach (ObjectId acObjId in acBlkTblRec)
                {
                    //获得块参照
                    if (acObjId.ObjectClass.DxfName != "INSERT") continue;
                    BlockReference block_reference;
                    block_reference = acTrans.GetObject(acObjId,
                                                        OpenMode.ForRead) as BlockReference;

                    WriteBlock2SQL(block_reference, index++, host_name, file_name, sql);
                }
            }

            //5——警告信息  
            CheckBlock2(host_name, file_name, "主射嘴", ref warning_messages);
            CheckBlock2(host_name, file_name, "中心定位柱", ref warning_messages);
            CheckBlock2(host_name, file_name, "定位销", ref warning_messages);
            CheckBlock2(host_name, file_name, "热电偶", ref warning_messages);
            CheckBlock2(host_name, file_name, "线架", ref warning_messages);
            if (warning_messages.Count > 0)
            {
                string str = "保存成功，但有如下警告，点击确定按钮完成\n";
                for (int i = 0; i < warning_messages.Count; i++) str += warning_messages[i] + "\n";
                MessageBox.Show(str, "警告！", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }

            acDoc.Editor.WriteMessage("\nCAD配置文件保存成功，有" + warning_messages.Count.ToString() + "个警告。\n");
            //4——解锁 完事
            docLock.Dispose();
        }
        catch (SystemException ex)
        {
            MessageBox.Show(ex.ToString());
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
    }

    [CommandMethod("ShortcutCommand")]
    public void ShortcutCommand()
    {
        App.ShowAlertDialog("保存图块数据：Sql\n生成所需图层：init\n注册：RegApp\n卸载：unRegApp");
    }

    [CommandMethod("ShowDllPath")]
    public void ShowDllPath()
    {
        string root_key = HostApplicationServices.Current.UserRegistryProductRootKey;

        RegistryKey reg_cad_key = Registry.CurrentUser.OpenSubKey(root_key);
        RegistryKey reg_app_key = reg_cad_key.OpenSubKey("Applications", true);

        //检查”ugbhrt” 键的版本
        string str_version = "0";
        string[] sub_keys = reg_app_key.GetSubKeyNames();
        foreach (string sub_key in sub_keys)
        {
            if (!sub_key.Equals(APP_NAME)) continue;

            RegistryKey reg_old_key = reg_app_key.OpenSubKey(sub_key, true);
            foreach (string value_name in reg_old_key.GetValueNames())
            {
                if (!value_name.Equals("VERSION")) continue;
                str_version = reg_old_key.GetValue(value_name).ToString();
            }
            if (str_version.Trim() == "") str_version = "0";
        }

        //获取本模块的位置（注册本程序自己）
        string dll_path = Assembly.GetExecutingAssembly().Location;

        App.ShowAlertDialog("2D.DLL插件地址：" + dll_path + "\n版本：" + str_version);
    }

    [CommandMethod("CreateManifold")]
    public void CreateManifold()
    {
        NetFunction.FilterType[] filters = new NetFunction.FilterType[1];
        NetFunction.FilterType lineType = NetFunction.FilterType.Line;
        filters[0] = lineType;
        DBObjectCollection objs = NetFunction.GetSelection("请选择流道中心线", filters);
        ManifoldBuilder builder = new ManifoldBuilder();
        builder.SetRunner(objs);
        builder.SimpleManifoldContour2();
    }

    [CommandMethod("NozzleEar")]
    public void NozzleEar()
    {
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database acCurDb = doc.Database;
        Editor editor = doc.Editor;

        //插入块定义
        string filePath = "D:\\0_CAD\\UG\\0_Project\\IMS\\Part_Base\\2D\\NozzleEar.dwg";
        string blockName = "NozzleEarBlock";
        acCurDb.ImportBlockFromDwg(filePath);

        Point3d pt = NetFunction.Pick("选择插入点");
        double angle = 0;//旋转角度
        Scale3d scale = new Scale3d(1);//比例

        //计算角度
        Point3d pt2 = NetFunction.Pick("选择第二点（确定方向）");
        Vector2d vec = new Vector2d(pt2.X - pt.X, pt2.Y - pt.Y);
        angle = vec.Angle + Math.PI / 2;

        //插入块参照
        ObjectId spaceId = acCurDb.CurrentSpaceId;
        DBObjectCollection objs = spaceId.InsertBlockReferenceThenExplore("0", blockName,
            pt, angle, scale);

        NetFunction.Add2BlockModelSpace(objs);

        //得到内圆弧
        List<Arc> arcs = new List<Arc>();
        foreach (Entity ent in objs)
        {
            if (ent.ObjectId.ObjectClass.DxfName.ToString() != "ARC") continue;
            Arc arc = (Arc)ent;
            if (arc.Radius > 400) arcs.Add(arc);
        }

        //得到分流板直线轮廓
        List<Line> lines = new List<Line>();
        DBObjectCollection newLines = new DBObjectCollection();
        ObjectIdCollection ids = NetFunction.GetIdsAtLayer("1manifold",
            NetFunction.FilterType.Line);
        using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
        {
            foreach (ObjectId id in ids)
            {
                Line line = trans.GetObject(id, OpenMode.ForWrite) as Line;
                if (line.Length < 70) continue;
                if (!line.to2d().IsParallelTo(new Line2d(Point2d.Origin,
                    new Point2d(vec.X, vec.Y))))
                    continue;
                lines.Add(line);
            }
            
            //计算交点
            Tolerance tolerance = new Tolerance(0.005, 0.005);
            foreach (Arc arc in arcs)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    Point2d[] points = arc.to2dCircle().IntersectWith(lines[i].segment(), 
                         tolerance);
                    if (points == null) continue;
                    if (points.Length != 2) continue;

                    List<Point3d> ps = new List<Point3d>();
                    ps.Add(lines[i].StartPoint);
                    ps.Add(points[0].to3d());
                    ps.Add(points[1].to3d());
                    ps.Add(lines[i].EndPoint);

                    //ps.OrderBy(su => su.X).ThenBy(su=>su.Y);
                    ps.Sort(PointCompare);

                    newLines.Add(new Line(ps[0], ps[1]));
                    newLines.Add(new Line(ps[2], ps[3]));

                    lines[i].Erase(true);
                    lines.RemoveAt(i);
                    break;
                }
            }
            trans.Commit();
        }

        newLines.Add2BlockModelSpace();

        
    }

    [CommandMethod("SimpleTube")]
    public void SimpleTube()
    {
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database acCurDb = doc.Database;
        Editor editor = doc.Editor;

        List<Point3d> ps = new List<Point3d>();
        
        PromptPointOptions optPoint = new PromptPointOptions("\n请选择第一个点:");
        optPoint.AllowNone = true;//Enter
        PromptPointResult resPoint = editor.GetPoint(optPoint);

        if (resPoint.Status != PromptStatus.OK) return;
        ps.Add(resPoint.Value);

        TubeJig jig = new TubeJig(ps);
        PromptResult resJig = editor.Drag(jig);
        
        while (resJig.Status == PromptStatus.OK && jig.finish==false)
        {
            jig.ps.Add(jig.tempPt);
            resJig = editor.Drag(jig);            
        }

        if (resJig.Status != PromptStatus.Cancel)
        {
            NetFunction.Add2BlockModelSpace(jig.results);
        }
    }

    #endregion

    public static int PointCompare(Point3d p1, Point3d p2)
    {
        int retval = 0;
        if (Math.Abs(p1.X - p2.X) < 0.001)
        {
            retval = p2.Y.CompareTo(p1.Y);
        }
        else
            retval = p2.X.CompareTo(p1.X);
        return retval;
    }

    #region 保存图块

    void WriteBlock2SQL(BlockReference block_reference, int index, String host_name, String file_name, DataControl sql)
    {
        try
        {
            String localdate = DateTime.Now.ToString("yyyyMMdd");
            String strSql = "insert into block_table(assembly_id,X,Y,Angle,host_name,file_name,local_date,block_index) Values(";
            strSql += "'" + block_reference.Name.Trim() + "',";
            strSql += Math.Round(block_reference.Position.X, 4).ToString() + ",";
            strSql += Math.Round(block_reference.Position.Y, 4).ToString() + ",";
            strSql += Math.Round(block_reference.Rotation, 6).ToString() + ",";
            strSql += "'" + host_name + "',";
            strSql += "'" + file_name + "',";
            strSql += localdate + ",";
            strSql += index.ToString() + ")";
            sql.dataExec(strSql);
        }
        catch (SystemException ex)
        {
            MessageBox.Show("保存数据的时候出错");
            MessageBox.Show(ex.ToString());
            return;
        }
    }

    /// <summary>
    /// 检查图层，将0图层设置为工作图层
    /// </summary>
    /// <param name="error_messages">错误信息列表</param>
    /// <param name="warning_nums">警告数</param>
    /// <param name="warning_messages">警告信息列表</param>
    /// <returns>错误数</returns>
    int CheckLayer(ref List<string> error_messages, ref int warning_nums, ref List<string> warning_messages)
    {
        try
        {
            int error_nums = 0;
            Document doc = App.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor editor = doc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                error_nums += CheckLayer(trans, db, editor, "1manifold", ref error_messages, ref warning_nums, ref warning_messages);
                error_nums += CheckLayer(trans, db, editor, "2runner", ref error_messages, ref warning_nums, ref warning_messages);
                error_nums += CheckLayer(trans, db, editor, "1submanifold", ref error_messages, ref warning_nums, ref warning_messages);
                error_nums += CheckLayer(trans, db, editor, "7gasline", ref error_messages, ref warning_nums, ref warning_messages);
                error_nums += CheckLayer(trans, db, editor, "3heater", ref error_messages, ref warning_nums, ref warning_messages);
                error_nums += CheckLayer(trans, db, editor, "wireframe", ref error_messages, ref warning_nums, ref warning_messages);

                LayerTable layer_table = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite);
                trans.Commit();
            }
            return error_nums;
        }
        catch (Autodesk.AutoCAD.Runtime.Exception ex)
        {
            MessageBox.Show(ex.ToString());
            return 0;
        }
    }

    int CheckLayer(Transaction trans, Database db, Editor editor, string layerName, ref List<string> error_messages, ref int warning_nums, ref List<string> warning_messages)
    {
        try
        {
            int error_nums = 0;

            LayerTable layer_table = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!layer_table.Has(layerName))
            {
                error_nums++;
                error_messages.Add("没有" + layerName + "图层");
            }
            else
            {
                LayerTableRecord ltr = trans.GetObject(layer_table[layerName],
                                                       OpenMode.ForRead) as LayerTableRecord;
                ObjectIdCollection ids = new ObjectIdCollection();
                PromptSelectionResult ProSset = null;
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, layerName) };
                SelectionFilter sfilter = new SelectionFilter(filList);
                ProSset = editor.SelectAll(sfilter);
                if (ProSset.Status == PromptStatus.OK)
                {
                    SelectionSet sst = ProSset.Value;
                    ObjectId[] oids = sst.GetObjectIds();
                    for (int i = 0; i < oids.Length; i++) ids.Add(oids[i]);
                }
                if (ids.Count == 0)
                {
                    warning_nums++;
                    warning_messages.Add(layerName + "图层中没有数据！");
                }
            }

            return error_nums;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.ToString());
            return 0;
        }
    }

    /// <summary>
    /// 检查图块，是否在asm图层中
    /// </summary>
    /// <param name="error_messages">错误信息列表</param>
    /// <param name="warning_nums">警告数</param>
    /// <param name="warning_messages">警告信息列表</param>
    /// <returns>错误数</returns>
    int CheckBlock(ref List<string> error_messages, ref int warning_nums, ref List<string> warning_messages)
    {
        int error_nums = 0;
        string[] work_layers = { "1manifold", "1submanifold", "2runner", "7gasline", "3heater" };

        //1——获得当前文档
        Document doc = App.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;
        Editor editor = doc.Editor;

        using (Transaction acTrans = db.TransactionManager.StartTransaction())
        {
            //2.1——以读模式打开Block表
            BlockTable block_tables;
            block_tables = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            //2.2——以读模式打开模型空间 BlockTable.ModelSpace
            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(block_tables[BlockTableRecord.ModelSpace],
                                                OpenMode.ForRead) as BlockTableRecord;

            //2.3——遍历模型空间ModelSpace
            foreach (ObjectId acObjId in acBlkTblRec)
            {
                //获得块参照
                if (acObjId.ObjectClass.DxfName != "INSERT") continue;
                BlockReference block_reference;
                block_reference = acTrans.GetObject(acObjId,
                                                    OpenMode.ForWrite) as BlockReference;
                if (!work_layers.Contains(block_reference.Layer)) continue;
                error_nums++;
                error_messages.Add("图块" + block_reference.Name + "不能保存在" + block_reference.Layer + "图层中");
                if (block_reference.Layer == "asm") continue;
                warning_nums++;
                warning_messages.Add("图块" + block_reference.Name + "应保存在asm图层中");
            }
        }

        return error_nums;
    }

    void CheckBlock2(string host_name, string file_name, string assemblyName, ref List<string> warning_messages)
    {
        try
        {
            DataControl sql = new DataControl();
            string strsql = "select count(*) as Expr1 from block_view where assembly_name='" + assemblyName + "'";
            strsql += " AND host_name='" + host_name + "' AND file_name='" + file_name + "'";
            int count = sql.GetExpr1AsInt(strsql);
            if (count == 0) warning_messages.Add("缺少图块：" + assemblyName);
        }
        catch (SystemException ex)
        {
            MessageBox.Show("检查图块的时候出错");
            MessageBox.Show(ex.ToString());
            return;
        }
    }

    #endregion
}

//用来响应按钮
class AdskCommandHandler : ICommand
{
    public bool CanExecute(object parameter)
    {
        return true;
    }

    public event EventHandler CanExecuteChanged;

    public void Execute(object parameter)
    {
        //is from Ribbon Button
        RibbonButton ribBtn = parameter as RibbonButton;
        if (ribBtn != null)
        {
            //execute the command 
            App.DocumentManager.MdiActiveDocument.SendStringToExecute((string)ribBtn.CommandParameter, true, false, true);
        }
    }
}
