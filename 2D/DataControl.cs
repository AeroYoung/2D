using System;
using System.Data;
using System.Configuration;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text;

class DataControl
{
    public DataSet dataSet;
    public String strCon;
    public String strIMS;

    #region API函数声明
    [DllImport("kernel32")]//返回0表示失败，非0为成功
    private static extern long WritePrivateProfileString(string section, string key,
        string val, string filePath);

    [DllImport("kernel32")]//返回取得字符串缓冲区的长度
    private static extern long GetPrivateProfileString(string section, string key,
        string def, StringBuilder retVal, int size, string filePath);
    #endregion

    public DataControl()
    {
        strIMS = Environment.GetEnvironmentVariable("ugims");
        String iniPath = strIMS + "\\StartUp\\IMS.ini";
        StringBuilder source = new StringBuilder(1024);
        StringBuilder user = new StringBuilder(1024);
        StringBuilder password = new StringBuilder(1024);
        GetPrivateProfileString("data", "localhost", "10.15.1.61", source, 1024, iniPath);
        strCon = "data source=" + source + "; Database=IMS;user id=sa; password=SAUCADCAM";
    }

    public int DataSelect(String strSql)
    {
        SqlDataAdapter dataAdapter;

        using (SqlConnection SqlConn = new SqlConnection())
        {
            SqlConn.ConnectionString = strCon;
            SqlConn.Open();

            dataAdapter = new SqlDataAdapter(strSql, SqlConn);
            dataSet = new DataSet();
            dataAdapter.Fill(dataSet);
        }

        return dataSet.Tables[0].Rows.Count;
    }

    public DataSet DataSelectGetSet(String strSql)
    {
        SqlDataAdapter dataAdapter;
        DataSet newdataSet;
        using (SqlConnection SqlConn = new SqlConnection())
        {
            SqlConn.ConnectionString = strCon;
            SqlConn.Open();

            dataAdapter = new SqlDataAdapter(strSql, SqlConn);
            newdataSet = new DataSet();
            dataAdapter.Fill(newdataSet);
        }

        return newdataSet;
    }

    public int GetExpr1AsInt(String strSql)
    {
        SqlDataAdapter dataAdapter;
        DataSet ds;
        using (SqlConnection SqlConn = new SqlConnection())
        {
            SqlConn.ConnectionString = strCon;
            SqlConn.Open();

            dataAdapter = new SqlDataAdapter(strSql, SqlConn);
            ds = new DataSet();
            dataAdapter.Fill(ds);
        }

        return int.Parse(ds.Tables[0].Rows[0]["Expr1"].ToString());
    }

    public String GetExpr1AsString(String strSql)
    {
        SqlDataAdapter dataAdapter;
        DataSet set = new DataSet();

        using (SqlConnection SqlConn = new SqlConnection())
        {
            SqlConn.ConnectionString = strCon;
            SqlConn.Open();

            dataAdapter = new SqlDataAdapter(strSql, SqlConn);
            set = new DataSet();
            dataAdapter.Fill(set);
        }
        if (set.Tables[0].Rows.Count > 0)
            return set.Tables[0].Rows[0]["Expr1"].ToString();
        else
            return "null";
    }

    public void ListViewShow(ListView lstName, string strtitle, string strwidth)
    {
        int intpos = 0, intlengh = 0;
        int intpos2 = 0, intlengh2 = 0;


        string[] str = new string[dataSet.Tables[0].Columns.Count];
        string[] strWi = new string[dataSet.Tables[0].Columns.Count];
        int j = 0, m = 0;


        while (intpos != -1)
        {
            intlengh = strtitle.Length;

            intpos = strtitle.IndexOf('|');

            if (intpos != -1)
                str[j] = strtitle.Substring(0, intpos);
            else
                str[j] = strtitle;

            strtitle = strtitle.Substring((intpos + 1), (intlengh - intpos - 1));
            j = j + 1;
        }

        while (intpos2 != -1)
        {
            intlengh2 = strwidth.Length;

            intpos2 = strwidth.IndexOf('|');

            if (intpos2 != -1)
                strWi[m] = strwidth.Substring(0, intpos2);
            else
                strWi[m] = strwidth;

            strwidth = strwidth.Substring((intpos2 + 1), (intlengh2 - intpos2 - 1));
            m = m + 1;
        }

        //

        lstName.Clear();
        lstName.Items.Clear();
        lstName.BeginUpdate();


        ColumnHeader ch = new ColumnHeader();
        for (int i = 0; i < dataSet.Tables[0].Columns.Count; i++)
            ch = lstName.Columns.Add((i + 1).ToString().Trim(), str[i], int.Parse(strWi[i]), HorizontalAlignment.Center, 0);//100 = gc.changeformat_int(strWi[i])
                                                                                                                            //
        for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
        {
            ListViewItem li = new ListViewItem();
            li.Text = dataSet.Tables[0].Rows[i][0].ToString().Trim();
            for (int k = 1; k < dataSet.Tables[0].Columns.Count; k++)
                li.SubItems.Add(dataSet.Tables[0].Rows[i][k].ToString().Trim());

            lstName.Items.Add(li);
        }
        //
        lstName.EndUpdate();
    }

    public void DataInsert(string strSql)
    {
        SqlCommand Comm;

        using (SqlConnection SqlConn = new SqlConnection())
        {
            SqlConn.ConnectionString = strCon;
            SqlConn.Open();
            Comm = new SqlCommand(strSql, SqlConn);


            if (SqlConn.State == 0)
                SqlConn.Open();
            Comm.ExecuteNonQuery();
            Comm.Dispose();
        }
    }

    /// <summary>
    /// 数据表删除函数
    /// </summary>
    /// <param name="strtablename">表名</param>
    /// <param name="strwhere">修改条件</param>
    /// <param name="intk">删除条件是否起作用intk=0，查询条件不起作用，intk=1删除条件起作用</param>
    public void dataDelete(string strtablename, string strwhere, int intk)
    {
        //

        SqlCommand Comm;
        string sqlStr1;

        if (intk != 0)
        {
            sqlStr1 = "delete " + strtablename + " where " + strwhere;
        }
        else
        {
            sqlStr1 = "delete " + strtablename;
        }



        using (SqlConnection SqlConn = new SqlConnection())
        {
            SqlConn.ConnectionString = strCon;
            SqlConn.Open();
            Comm = new SqlCommand(sqlStr1, SqlConn);


            if (SqlConn.State == 0)
                SqlConn.Open();
            Comm.ExecuteNonQuery();
            Comm.Dispose();

        }
    }

    /// <summary>
    /// 执行没有返回的数据库操作
    /// </summary>
    /// <param name="strSql">语句</param>
    public void dataExec(string strSql)
    {
        SqlCommand Comm;
        using (SqlConnection SqlConn = new SqlConnection())
        {
            SqlConn.ConnectionString = strCon;
            SqlConn.Open();
            Comm = new SqlCommand(strSql, SqlConn);


            if (SqlConn.State == 0)
                SqlConn.Open();
            Comm.ExecuteNonQuery();
            Comm.Dispose();

        }
    }

    /// <summary>
    /// DataTable导入BOM_Current_table
    /// </summary>
    /// <param name="system_code">system_code</param>
    /// <param name="dt">数据源</param>
    public void DataTable2SQL(DataTable dt)
    {
        using (SqlBulkCopy sqlRevdBulkCopy = new SqlBulkCopy(strCon))
        {
            sqlRevdBulkCopy.DestinationTableName = "BOM_Current_table";
            sqlRevdBulkCopy.NotifyAfter = dt.Rows.Count;//有几行数据 
            sqlRevdBulkCopy.ColumnMappings.Add("system_code", "system_code");
            sqlRevdBulkCopy.ColumnMappings.Add("contract_code", "contract_code");
            sqlRevdBulkCopy.ColumnMappings.Add("mould_code", "mould_code");
            sqlRevdBulkCopy.ColumnMappings.Add("node_id", "node_id");
            sqlRevdBulkCopy.ColumnMappings.Add("parent_id", "parent_id");
            sqlRevdBulkCopy.ColumnMappings.Add("num", "num");
            sqlRevdBulkCopy.ColumnMappings.Add("code", "code");
            sqlRevdBulkCopy.ColumnMappings.Add("display_text", "display_text");
            sqlRevdBulkCopy.WriteToServer(dt);//数据导入数据库 
            sqlRevdBulkCopy.Close();//关闭连接  
        }
    }

    /// <summary>
    /// 将dt导入到和standard_table结构完全相同的表中
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="temp_table_name">临时表的名称</param>
    public void DataTable2TempStandardTable(DataTable dt, String temp_table_name)
    {
        using (SqlBulkCopy sqlRevdBulkCopy = new SqlBulkCopy(strCon))
        {
            sqlRevdBulkCopy.DestinationTableName = temp_table_name;
            sqlRevdBulkCopy.NotifyAfter = dt.Rows.Count;//有几行数据 
            sqlRevdBulkCopy.ColumnMappings.Add("st_code", "st_code");
            sqlRevdBulkCopy.ColumnMappings.Add("series", "series");
            sqlRevdBulkCopy.ColumnMappings.Add("st_material_name", "st_material_name");
            sqlRevdBulkCopy.ColumnMappings.Add("st_specification", "st_specification");
            sqlRevdBulkCopy.ColumnMappings.Add("st_material", "st_material");
            sqlRevdBulkCopy.ColumnMappings.Add("st_brand", "st_brand");
            sqlRevdBulkCopy.ColumnMappings.Add("st_ORI", "st_ORI");
            sqlRevdBulkCopy.ColumnMappings.Add("st_remark", "st_remark");
            sqlRevdBulkCopy.ColumnMappings.Add("st_group_code", "st_group_code");
            //st_spare和num不要
            sqlRevdBulkCopy.WriteToServer(dt);//数据导入数据库 
            sqlRevdBulkCopy.Close();//关闭连接  
        }
    }
}