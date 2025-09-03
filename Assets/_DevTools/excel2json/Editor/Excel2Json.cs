using UnityEditor;
using System;
using System.IO;
using System.Data;
using System.Text;
using UnityEngine;

namespace excel2json
{

    public class Excel2Json 
    {

        [MenuItem("Tools/Excel2Json")]
        static void Run()
        {
            
            LoadExcelAndExport("GameSetting", "GameSetting", new string[] { "Item", "Activity" });
        }

        public static void LoadExcelAndExport(string excel_file_name, string json_file_name, string[] dictSheetName = null)
        {
            //-- Load Excel
            string excelPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(excel_file_name)[0]);
            ExcelLoader excel = new ExcelLoader(excelPath, 3);

            //-- export
            JsonExporter exporter = new JsonExporter(excel, false, true, "yyyy/MM/dd", false, 3, "#", true, false, dictSheetName);
            exporter.SaveToFile("Assets/Resources/LocationJson/" + json_file_name +".json", Encoding.UTF8);
            AssetDatabase.Refresh();    //刷新数据

            Debug.Log("excel2json操作已完成:" + excel_file_name);
        }

        public static void LoadAllExcelAndExport(string[] excel_file_names)
        {
            for (int i = 0; i < excel_file_names.Length; i++)
            {
                LoadExcelAndExport(excel_file_names[i], excel_file_names[i]);
            }
        }
    }
}

