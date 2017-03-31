using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections.Generic;

namespace excel2json
{
    /// <summary>
    /// 根据表头，生成Java类定义数据结构
    /// 表头使用三行定义：字段名称、字段类型、注释
    /// </summary>
    class JavaDefineGenerator
    {
        struct FieldDef
        {
            public string name;
            public string type;
            public string comment;
        }

        List<FieldDef> m_fieldList;

        public String ClassComment
        {
            get;
            set;
        }

        public JavaDefineGenerator(DataTable sheet)
        {
            //-- First Row as Column Name
            if (sheet.Rows.Count < 2)
                return;

            m_fieldList = new List<FieldDef>();
            DataRow typeRow = sheet.Rows[0];
            DataRow commentRow = sheet.Rows[1];

            foreach (DataColumn column in sheet.Columns)
            {
                FieldDef field;

                var fieldName = column.ToString().Trim();
                if (fieldName.StartsWith("R_"))
                {
                    fieldName = fieldName.Replace("R_", "");
                }


                if (fieldName.StartsWith("QP_"))
                {
                    fieldName = fieldName.Replace("QP_", "");
                }

                if (fieldName.StartsWith("MP_"))
                {
                    fieldName = fieldName.Replace("MP_", "");
                }

                if (fieldName.StartsWith("P_"))
                {
                    fieldName = fieldName.Replace("P_", "");
                }

                field.name = fieldName;
                field.type = typeRow[column].ToString().Trim();
                field.comment = commentRow[column].ToString().Trim();

                m_fieldList.Add(field);
            }
        }

        public string ConvertJavaDataType(string type)
        {
            type = type.ToLower();
            switch (type)
            {
                case "integer":
                    return "Integer";
                case "string":
                    return "String";
                case "date":
                    return "Date";
                case "double":
                    return "Double";
                default:
                    return type;
            }
        }


        public void SaveToFile(string filePath, Encoding encoding)
        {
            if (m_fieldList == null)
                throw new Exception("JavaDefineGenerator内部数据为空。");

            string defName = Path.GetFileNameWithoutExtension(filePath);

            //-- 创建代码字符串
            StringBuilder sbForEntity = new StringBuilder();
            StringBuilder sbForEntityToString = new StringBuilder();
            sbForEntity.AppendLine("package Models;");
            sbForEntity.AppendLine();
            sbForEntity.AppendLine("import java.util.*;");
            sbForEntity.AppendLine();
            sbForEntity.AppendFormat("public class {0}\r\n{{", defName + "Entity");
            sbForEntity.AppendLine();
            foreach (FieldDef field in m_fieldList)
            {
                sbForEntity.AppendFormat("\tpublic {0} {1}; // {2}", ConvertJavaDataType(field.type), field.name, field.comment);

                sbForEntityToString.AppendLine("\t\tif(" + field.name + " == null){sb.append(\"" + field.name + "=null<br>\");} else {sb.append(\"" + field.name + " = \" + " + field.name + ".toString() + \"<br>\");}");

                //sbForEntityToString.AppendLine("\t\tsb.append(\"" + field.name + "=\" + " + field.name + " == null ? \"null\" : "+ field.name + ".toString() + \"\\n\");");
                sbForEntity.AppendLine();
            }

            sbForEntity.AppendLine();
            sbForEntity.AppendLine();
            sbForEntity.AppendLine("\t@Override");
            sbForEntity.AppendLine("\tpublic String toString() {");
            sbForEntity.AppendLine("\t\tStringBuilder sb=new StringBuilder();");
            sbForEntity.Append(sbForEntityToString.ToString());
            sbForEntity.AppendLine("\t\treturn sb.toString();");
            sbForEntity.AppendLine("\t}");
            sbForEntity.AppendLine("}");
            sbForEntity.AppendLine();
            //-- 保存文件
            String filePath1 = filePath.Replace(defName, defName + "Entity");
            using (FileStream file = new FileStream(filePath1, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter writer = new StreamWriter(file, encoding))
                    writer.Write(sbForEntity.ToString());
            }



            StringBuilder sb = new StringBuilder();
            sb.AppendLine("package Models;");
            sb.AppendLine();
            sb.AppendLine("import Utils.FileHelper;");
            sb.AppendLine("import Utils.GsonHelper;");
            sb.AppendLine();
            sb.AppendLine("import java.util.*;");
            sb.AppendLine();
            if (this.ClassComment != null)
                sb.AppendLine(this.ClassComment);
            sb.AppendLine("public class " + defName + " implements IDataPointCalc {");
            sb.AppendLine();
            sb.AppendLine("\tprivate String TestCasePath;");
            sb.AppendLine("\tpublic List<" + defName + "Entity" + "> TestCaseEntities;");
            sb.AppendLine("\tprivate HashMap map;");
            sb.AppendLine();
            sb.AppendLine("\tpublic " + defName + "(String testcasepath) {");
            sb.AppendLine("\t\tTestCasePath = testcasepath;");
            sb.AppendLine("\t\tString JsonContent = FileHelper.ReadFileContent(testcasepath);");
            sb.AppendLine("\t\tTestCaseEntities = GsonHelper.convertEntities(JsonContent, " + defName + "Entity.class);");
            sb.AppendLine("\t}");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("\tpublic void GenerateMockData() {");
            sb.AppendLine("\t\tint size = TestCaseEntities.size();");
            sb.AppendLine("\t\tif (size > 0) {");
            sb.AppendLine("\t\t\tfor (int i = 0; i < size; i++) {");
            sb.AppendLine("\t\t\t\t" + defName + "Entity entity = TestCaseEntities.get(i);");
            sb.AppendLine("\t\t\t\t//TODO: Write Code at here.Save data into DynamoDB or Kinesis");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("\t public HashMap<Integer, Boolean> ValidateMockData() {");
            sb.AppendLine("\t\tint size = TestCaseEntities.size();");
            sb.AppendLine("\t\tif (size > 0) {");
            sb.AppendLine("\t\t\tmap = new HashMap();");
            sb.AppendLine("\t\t\tfor (int i = 0; i < size; i++) {");
            sb.AppendLine("\t\t\t\t" + defName + "Entity entity = TestCaseEntities.get(i);");
            sb.AppendLine("\t\t\t\t//TODO: Write Validate Code at here.");
            sb.AppendLine("\t\t\t\t");
            sb.AppendLine("\t\t\t\tBoolean ValidateResult = false;");
            sb.AppendLine("\t\t\t\tmap.put(entity.No, ValidateResult);");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\treturn map;");
            sb.AppendLine("\t}");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("\t@Override");
            sb.AppendLine("\tpublic String OutputTestResult(HashMap<Integer, Boolean> result)");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tStringBuilder sb = new StringBuilder();");
            sb.AppendLine("\t\tsb.append(\" <div><table border='1' cellspacing='0' bordercolor='#000000' width = '100%' style='border-collapse:collapse;'><tr><td colspan='3'>\" + this.getClass().getSimpleName() + \"</td></tr>\"); ");
            sb.AppendLine("\t\tString Format = \" <tr><td>%s</td><td>%s</td><td>%s</td></tr>\";");
            sb.AppendLine("\t\tList<Integer> list = new ArrayList<>(result.keySet());");
            sb.AppendLine("\t\tfor (int i = 0; i < list.size(); i++)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tint No = list.get(i);");
            sb.AppendLine("\t\t\tfor (int j = 0; j < result.size(); j++)");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\t" + defName + "Entity entity = TestCaseEntities.get(j);");
            sb.AppendLine("\t\t\t\tif (No == entity.No)");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\tsb.append(String.format(Format, entity.No, result.get(entity.No), entity.toString()));");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\tsb.append(\"</table></div>\");");
            sb.AppendLine("\t\treturn sb.toString();");
            sb.AppendLine("\t}");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("}");


            //-- 保存文件
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter writer = new StreamWriter(file, encoding))
                    writer.Write(sb.ToString());
            }
        }
    }
}
