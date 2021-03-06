﻿namespace Mysoft.Business.Validation
{
    using Microsoft.Data.Schema.ScriptDom;
    using Microsoft.Data.Schema.ScriptDom.Sql;
    using Mysoft.Business.Validation.Db;
    using Mysoft.Map.Extensions.DAL;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class CommonValidation
    {
        private static readonly Regex EqualRegex = new Regex(@"[1-4]+\s*=\s*[1-4]+", RegexOptions.IgnoreCase);

        /// <summary>
        /// 校验SQL语句
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static bool IsIncorrectSql(string sql)
        {
            TSql100Parser parser = new TSql100Parser(false);
            IList<ParseError> errors = new List<ParseError>();
            using (StringReader reader = new StringReader(sql))
            {
                parser.Parse(reader, out errors);
                return (errors.Count > 0);
            }
        }

        public static bool CheckCase(string sql, int pagemode, out string error)
        {
            //有的牛B开发吃透了MAP，玩法灵活，用子查询来实现排序，因此以下的检测反而不可靠

            int start = sql.IndexOf("(");
            error = "";
            if (start >= 0)
            {
                string subsql = sql.Substring(start);
                //若包含子查询
                if (subsql.Trim().IndexOf("select", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string temp = sql.Substring(0, start);
                    //外层查询必须大写
                    if (Regex.IsMatch(temp, "(select|from|where)", RegexOptions.CultureInvariant))
                    {
                        error = "外层查询关键字非大写，建议修改";
                        return false;
                    }

                    int end = sql.IndexOf(")", start);
                    //如果子查询使用了大写
                    if (Regex.IsMatch(sql.Substring(start, end - start), "(SELECT|FROM|WHERE)",
                                      RegexOptions.CultureInvariant))
                    {
                        error = "子查询关键字非小写，建议修改";
                        return false;
                    }

                    //如果SQL语句最后，最外层使用了小写
                    if (Regex.IsMatch(sql.Substring(end), "(order|by|where)", RegexOptions.CultureInvariant))
                    {
                        error = "外层查询条件关键字非大写，建议修改";
                        return false;
                    }
                }
            }
            return true;
        }

        public static void ExecuteSql(string sql)
        {
            SqlConnection conn = new SqlConnection(DbAccessManager.Connectstring);
            try
            {
                conn.Open();
                new SqlCommand(sql, conn).ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                conn.Close();
            }
        }

        public static string GetFilter(string sql)
        {
            string temp = EqualRegex.Replace(sql, "1=2");
            return DbAccessManager.Keyword.Aggregate<string, string>(temp, (current, k) => current.Replace(k, "(null)")).Replace("[授权系统]", "select application from myapplication").Replace("[用户所属公司及下级公司过滤]", "''");
        }

        public static string GetPageSqlByNotIn(string sql, string entity, string primaryKey)
        {
            string strSql = sql.Substring(sql.IndexOf("SELECT ") + 6);
            strSql = "SELECT TOP 10 " + strSql;
            if (string.IsNullOrEmpty(primaryKey))
            {
                return sql;
            }
            string strTemp = (string.IsNullOrEmpty(entity) ? "" : (entity + ".")) + primaryKey.Replace("'", "''") + " NOT IN (SELECT TOP 10 " + (string.IsNullOrEmpty(entity) ? "" : (entity + ".")) + primaryKey.Replace("'", "''") + " " + Regex.Replace(strSql, @".*?FROM\b", "FROM", RegexOptions.Singleline) + ")";
            if (strSql.LastIndexOf("WHERE ") > 0)
            {
                return strSql.Replace("WHERE ", "WHERE " + strTemp + " AND ");
            }
            if (strSql.IndexOf("ORDER BY ") > 0)
            {
                return strSql.Replace("ORDER BY ", "WHERE " + strTemp + " ORDER BY ");
            }
            return (strSql + " WHERE " + strTemp);
        }

        public static string GetPageSqlByRowNumber(string strSql)
        {
            int index = strSql.LastIndexOf("ORDER BY");
            strSql = "WITH _t AS (SELECT ROW_NUMBER() OVER(" + strSql.Substring(index) + ") AS _RowNumber," + strSql.Substring(0, index) + ") SELECT * FROM _t WHERE _RowNumber BETWEEN 0 AND 10 ORDER BY _RowNumber";
            return strSql;
        }

        public static bool HasSqlKeywords(string sql)
        {
            return Regex.IsMatch(sql, @"[^\w]+(option|COMPUTE)[^\w]+", RegexOptions.IgnoreCase);
        }

        public static MssqlVersion GetDbVersion()
        {
            string version = CPQuery.From("select ServerProperty('Productversion')").ExecuteScalar<string>();
            string ver = version.Substring(0, version.IndexOf('.'));
            switch (ver)
            {
                case "8":
                    return MssqlVersion.SQL2000;
                case "9":
                    return MssqlVersion.SQL2005;
                case "10":
                    return MssqlVersion.SQL2008;
                case "11":
                    return MssqlVersion.SQL2012;
                default:
                    throw new NotSupportedException("不支持此数据库版本：" + version);
            }
        }
    }

    public enum MssqlVersion
    {
        SQL2000,
        SQL2005,
        SQL2008,
        SQL2012,
    }
}