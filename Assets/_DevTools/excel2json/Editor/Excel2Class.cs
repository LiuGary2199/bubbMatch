using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace excel2json
{
    public class Excel2Class
    {
        [MenuItem("Tools/Excel2Class")]
        static void Run()
        {
            // 生成基础数据结构
            LoadAndGenerate("GameSetting", "GameDB", new string[] { "ResourceDB", "SettingDB", "ActivityCtrlDB" }, new string[]{ "Item", "GameSetting", "Activity" });
        }

        /// <summary>
        /// 根据Setting配置，生成基础数据结构代码
        /// </summary>
        /// <param name="excel_file_name"></param>
        /// <param name="class_name"></param>
        public static void LoadAndGenerate(string excel_file_name, string class_name, string[] item_class_names, string[] item_sheet_names)
        {
            //-- Load Excel
            string excelPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(excel_file_name)[0]);
            ExcelLoader excel = new ExcelLoader(excelPath, 3);

            // generate - item
            string extCode = "";
            if (item_sheet_names != null && item_sheet_names.Length >0)
            {
                for (int i = 0; i < item_sheet_names.Length; i++)
                {
                    ItemCSGenerator itemGenerator = new ItemCSGenerator(excel, excel_file_name, item_sheet_names[i], item_class_names[i]);
                    extCode += itemGenerator.code;
                }
            }
            
            // generate - base
            CSDefineGenerator generator = new CSDefineGenerator(excel_file_name, excel, "#", extCode);
            generator.SaveToFile("Assets/CommonTool/Framework/Data/" + class_name + ".cs", System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();    //刷新数据

            Debug.Log("excel2class操作 - 生成基础数据结构代码已完成:" + excel_file_name);
        }
    }

}
