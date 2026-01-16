using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Threading;

namespace MSSQL
{
    class MSSQLHandler
    {
        public string server { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public bool useWindowsAuth { get; set; }
        private SqlConnection connect;

        public MSSQLHandler(string _server)
        {
            server = _server;
            useWindowsAuth = true;
        }

        public MSSQLHandler(string _ip, string _port, string _user, string _password)
        {
            ip = _ip;
            port = _port;
            server = $"{ip},{port}";
            user = _user;
            password = _password;
            useWindowsAuth = false;
        }

        /// <summary>
        /// string connectionString = "Server=localhost\\MSSQL2022;Trusted_Connection=True;";<br/>
        /// string connectionString = @"Server=192.168.0.100,1433; User Id=chimingkuei; Password=Asher19910930;"; // 遠端連線<br/>
        /// </summary>
        public string SqlConnectionString()
        {
            string dataSource;
            if (useWindowsAuth)
            {
                if (string.IsNullOrWhiteSpace(server))
                {
                    MessageBox.Show("請輸入伺服器名稱!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
                dataSource = server;
                return $"Server={dataSource};Trusted_Connection=True;";
            }
            else
            {
                // 遠端 SQL 驗證
                if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port))
                {
                    MessageBox.Show("請輸入伺服器名稱!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
                dataSource = $"{ip},{port}";
                return $"Server={dataSource};User Id={user};Password={password};";
            }
        }

        public bool Connect()
        {
            string connectionString = SqlConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;
            try
            {
                connect = new SqlConnection(connectionString);
                connect.Open();
                Log.Information("✅ 成功連線至 SQL Server。");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("❌ 無法連線：{Error}", ex.Message);
                MessageBox.Show($"無法連線資料庫：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool CheckConnect()
        {
            if (connect == null || connect.State != ConnectionState.Open)
            {
                Log.Warning("⚠️ 尚未連線資料庫，請先呼叫 Connect()");
                return false;
            }
            return true;
        }

        public void Disconnect()
        {
            if (connect == null)return;
            if (connect.State == ConnectionState.Open)
            {
                connect.Close();
                Log.Information("🔒 資料庫連線已關閉。");
            }
        }

        #region Show Database Or Table Information
        public string ShowDatabase(string databaseName, string tableName)
        {
            return "select name from sys.databases;";
        }

        public string ShowTable(string databaseName, string tableName)
        {
            return "use " + databaseName + ";" + 
                   "select name from sys.tables;";
        }

        public string ShowTableData(string databaseName, string tableName)
        {
            return "use " + databaseName + ";" +
                   "select * from " + tableName + ";";
        }

        public void ReadNameList(SqlDataReader reader)
        {
            while (reader.Read())
            {
                Console.WriteLine($"{reader["name"]}");
            }
        }

        public void ReadDataList(SqlDataReader reader)
        {
            // 取得欄位名稱
            var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            // 印出欄位標題列
            Console.WriteLine(string.Join(" | ", columnNames));
            Console.WriteLine(new string('-', 100));
            // 逐列印出資料
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == DBNull.Value)
                        values[i] = "NULL";
                    else if (values[i] is decimal dec)
                        values[i] = dec.ToString("N2");
                    else if (values[i] is DateTime dt)
                        values[i] = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
                Console.WriteLine(string.Join(" | ", values));
            }
        }

        public void DatabaseOrTableInform(string databaseName, string tableName, Func<string, string, string> fun)
        {
            if (!CheckConnect()) return;
            try
            {
                using (var cmd = new SqlCommand(fun(databaseName, tableName), connect))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.OutputEncoding = System.Text.Encoding.UTF8;
                    switch (fun.Method.Name)
                    {
                        case "ShowDatabase":
                            Log.Information("顯示所有資料庫。");
                            Console.WriteLine("📚 資料庫列表：");
                            ReadNameList(reader);
                            break;
                        case "ShowTable":
                            Log.Information("顯示所有資料表。");
                            Console.WriteLine("📋 資料表列表：");
                            ReadNameList(reader);
                            break;
                        case "ShowTableData":
                            Log.Information("顯示所有資料。");
                            Console.WriteLine("📊 資料列表：");
                            ReadDataList(reader);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("❌ 查詢資料失敗：{Error}", ex.Message);
            }
        }
        #endregion

        #region Database Operation
        public string CreateDatabase(string databaseName)
        {
            return $"create database [{databaseName}];";
        }

        public string DropDatabase(string databaseName)
        {
            return $"drop database [{databaseName}];";
        }

        public void DatabaseOperate(string databaseName, Func<string, string> fun)
        {
            if (!CheckConnect()) return;
            try
            {
                using (var cmd = new SqlCommand(fun(databaseName), connect))
                {
                    cmd.ExecuteNonQuery();
                    if (fun.Method.Name == "CreateDatabase")
                        Log.Information("✅ 資料庫 {databaseName} 已建立。", databaseName);
                    else
                        Log.Information("✅ 資料庫 {databaseName} 已刪除。", databaseName);
                }
            }
            catch (Exception ex)
            {
                Log.Error("❌ 增加或刪除資料庫失敗：{Error}", ex.Message);
            }
        }
        #endregion

        #region Table Operation
        public string CreateTable(string databaseName, string tableName, string content)
        {
            return "use " + databaseName + ";" +
                   "create table " + tableName + "(" + content + ");";
        }

        public string DropTable(string databaseName, string tableName, string content)
        {
            return "use " + databaseName + ";" +
                   "drop table " + tableName + ";";
        }

        public string InsertData(string databaseName, string tableName, string content)
        {
            return "use " + databaseName + ";" +
                   "insert into " + tableName + " " + content + ";";
        }

        public string UpdateData(string databaseName, string tableName, string content)
        {
            return "use " + databaseName + ";" +
                   "update " + tableName + " " + content + ";";
        }

        public string DeleteData(string databaseName, string tableName, string content)
        {
            return "use " + databaseName + ";" +
                   "delete from " + tableName + " " + content + ";";
        }

        public void TableOperate(string databaseName, string tableName, string content, Func<string, string, string, string> fun)
        {
            if (!CheckConnect()) return;
            try
            {
                using (var cmd = new SqlCommand(fun(databaseName, tableName, content), connect))
                {
                    cmd.ExecuteNonQuery();
                    switch (fun.Method.Name)
                    {
                        case "CreateTable":
                            Log.Information("✅ 資料表 {tableName} 已建立。", tableName);
                            break;
                        case "DropTable":
                            Log.Information("✅ 資料表 {tableName} 已刪除。", tableName);
                            break;
                        case "InsertData":
                            Log.Information("✅ 資料表 {tableName} 已增加資料。", tableName);
                            break;
                        case "UpdateData":
                            Log.Information("✅ 資料表 {tableName} 已更新資料。", tableName);
                            break;
                        case "DeleteData":
                            Log.Information("✅ 資料表 {tableName} 已刪除資料。", tableName);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("❌ Table操作失敗：{Error}", ex.Message);
                switch (fun.Method.Name)
                {
                    case "InsertData":
                        Log.Error("語法提醒︰(名稱1, 名稱2...) values (Data1, Data2...)");
                        break;
                    case "UpdateData":
                        Log.Error("語法提醒︰set 名稱1=value1,... where 條件式");
                        break;
                    case "DeleteData":
                        Log.Error("語法提醒︰where 條件式");
                        break;
                }
            }
        }
        #endregion

    }
}
