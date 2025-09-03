using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace excel2json
{
    /// <summary>
    /// 根据配置，按行生成类
    /// </summary>
    class ItemCSGenerator
    {
        string mCode;

        public string code
        {
            get
            {
                return this.mCode;
            }
        }

        public ItemCSGenerator(ExcelLoader excel, string excelName, string sheetName, string csName, int mHeaderRows = 3)
        {
            //-- 创建代码字符串
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("/// <summary>");
            sb.AppendFormat("/// 1. 资源类为名为'{0}'的Sheet中的配置", sheetName);
            sb.AppendLine();
            sb.AppendLine("/// 2. 表格约定：id为属性名称，value_type为属性类型，comment为注释");
            sb.AppendFormat("/// Generate From {0}.xlsx -> Sheet[{1}]", excelName, sheetName);
            sb.AppendLine();
            sb.AppendLine("/// </summary>");
            sb.AppendFormat("public class {0}\r\n{{", csName);
            sb.AppendLine();
            for (int i = 0; i < excel.Sheets.Count; i++)
            {
                DataTable sheet = excel.Sheets[i];
                if (sheet.TableName == sheetName)
                {
                    sb.Append(_exportRows(sheet, mHeaderRows));
                }
            }

            sb.Append('}');
            sb.AppendLine();

            mCode = sb.ToString();
        }

        private string _exportRows(DataTable sheet, int mHeaderRows)
        {
            // export as string
            StringBuilder sb = new StringBuilder();

            for (int i = mHeaderRows - 1; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];

                sb.AppendFormat("\tpublic {0} {1} {{ get; set; }} // {2}", row["value_type"].ToString(), row["id"].ToString(), row["comment"].ToString());
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}

