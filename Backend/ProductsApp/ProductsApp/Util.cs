﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Oracle.ManagedDataAccess.Client;
using ProductsApp;

namespace Utility
{
    public class Util
    {
        // GET api/values/5  
        //给出用户id或动态id

        public static Tuple<ByteArrayContent,string> Get(string id,int type)
        {
            //打开数据库
            OracleConnection conn = new OracleConnection(DBAccess.connStr);
            conn.Open();
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = conn;


            //取图片全路径
            if (type == 1)
            {
                cmd.CommandText = "select URL from PICTURE where ID='" + id + "'";  //多路径
            }
            else if (type == 2)
            {
                cmd.CommandText = "select PHOTO from USERS where ID='" + id + "'";
            }
            OracleDataReader rd = cmd.ExecuteReader();

            string filePath="";

            if (rd.Read())
            {
                filePath = rd[0].ToString();
            }
            

            //本地读文件
            if (filePath.Trim().Equals(""))
            {
                return null;
            }

            //图片文件转字节流
            MemoryStream ms = new MemoryStream();


            //分割后缀
            string fileExt = Path.GetExtension(filePath);

            //判断后缀
            ImageFormat format = null;
            switch (fileExt.ToLower())
            {
                case ".png":
                    format = ImageFormat.Png;
                    break;
                case ".bmp":
                    format = ImageFormat.Bmp;
                    break;
                case ".jpeg":
                    format = ImageFormat.Jpeg;
                    break;
                case ".jpg":
                    format = ImageFormat.Jpeg;
                    break;
            }

            Image img = Image.FromFile(filePath);  //这里我把路径给出了，他只用给我文件名
            //从格式图像转成字节流
            img.Save(ms, format);
            img.Dispose();
            rd.Close();
            conn.Close();

            //字节流转回前端字节数组
            return new Tuple<ByteArrayContent,string>(new ByteArrayContent(ms.ToArray()),fileExt); //字节流转数组

        }


        public static List<string> GetPid(string Mid)
        {
            List<String> list = new List<String>();
            //开库
            OracleConnection conn = new OracleConnection(DBAccess.connStr);
            conn.Open();

            //
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandText = "select ID from PICTURE where MOMENT_ID='" + Mid + "' ";

            OracleDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(rd[0].ToString());
            }

            rd.Close();
            conn.Close();

            return list;
      
        }

        //post api/values  
        //动态加组图（动态ID）or 个人信息修改加头图（用户ID） 
        public static bool Post(string id,int type)
        {
            bool flag = true;
           
            //打开数据库
            OracleConnection conn = new OracleConnection(DBAccess.connStr);
            conn.Open();
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = conn;
            //浏览器上传文件的body
            var SrcRequest = HttpContext.Current.Request;


            string filepath = "";
            //逐一存下body中给出的文件集合
            foreach (string f in SrcRequest.Files.AllKeys)
            {
                HttpPostedFile file = SrcRequest.Files[f];
                //分割后缀
                string fileExt = Path.GetExtension(file.FileName);

                if (type == 1)
                {
                    //插入PICTURE一条记录
                    GeneralAPI g = new GeneralAPI();

                    string pid = g.NewIDOf("PICTURE");
                    //文件路径不重名
                    filepath = @"C:\mmps\" + pid + fileExt;
                    cmd.CommandText = "insert into PICTURE(ID,URL,MOMENT_ID) values('" + pid+ "','" + filepath + "','" + id + "')";
                    if (cmd.ExecuteNonQuery() != 1)
                    {
                       flag = false;
                    }
                }
                else if (type == 2)
                {
                    //更新用户个人头像
                    filepath = @"C:\heads\" + id + fileExt;
                    cmd.CommandText = "update USERS set PHOTO='" + filepath + "' where ID='" + id + "'";
                    if (cmd.ExecuteNonQuery() != 1)
                    {
                        flag = false;
                    }
                }

                //存本地
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
                file.SaveAs(filepath);
            }
            return flag;
        }

    }

}

