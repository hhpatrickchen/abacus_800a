using Sopdu.StripMapVision;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Sopdu.helper
{
    public class DBAccess
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool InsertRecipeName(string RecipeName, string Blocks, string row, string col, string userid, string yield)
        {
            try
            {
                DatabaseStr dbstr = new DatabaseStr();
                DatabaseFunction db = new DatabaseFunction();
                db.ExecuteNonQuery(dbstr.InsertRecipeName(RecipeName, Blocks, row, col, userid, yield));
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return false;
            }
            return true;
        }

        public bool InserLFInv(string RecipeName, string LFInv, string userid)
        {
            try
            {
                DatabaseStr dbstr = new DatabaseStr();
                DatabaseFunction db = new DatabaseFunction();
                db.ExecuteNonQuery(dbstr.InsertLFInv(RecipeName, LFInv, userid));
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return false;
            }
            return true;
        }

        public Substrate GetSubstrateData(string RecipeName)
        {
            Substrate rst = null;
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            DataTable tbrecipe = db.ExecuteQueryCmd(str.SelectRecipeName(RecipeName));
            DataTable tbinvlist = db.ExecuteQueryCmd(str.SelectInvList(RecipeName));
            if (tbrecipe.Rows.Count > 0)
            {
                rst = new Substrate();
                foreach (DataRow row in tbrecipe.Rows)
                {
                    rst.RecipeName = RecipeName;
                    rst.numBlock = row.Field<int>("Blocks");
                    rst.row = row.Field<int>("bRow");
                    rst.column = row.Field<int>("bCol");
                    rst.yield = row.Field<double>("yield");
                }
            }
            if (tbinvlist.Rows.Count > 0)
            {
                rst.IFInvList = new System.Collections.ObjectModel.ObservableCollection<string>();
                foreach (DataRow row in tbinvlist.Rows)
                {
                    rst.IFInvList.Add(row.Field<string>("LFInvList"));
                }
            }
            return rst;
        }

        public string GetRecipeFromLFInv(string LFInv)
        {
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            DataTable dt = db.ExecuteQueryCmd(str.GetRecipeFromLFInv(LFInv));
            string recipename = dt.Rows[0].Field<string>("RecipeName");
            return recipename.Trim();
        }

        public void RemoveRecipe(string RecipeName)
        {
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            db.ExecuteNonQuery(str.RemoveRecipe(RecipeName));
        }

        public void RemoveInvName(string InvName)
        {
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            db.ExecuteNonQuery(str.RemoveInvF(InvName));
        }

        internal DataTable GetRecipeList()
        {
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            return db.ExecuteQueryCmd(str.GetRecipeList());

            //   throw new NotImplementedException();
        }

        internal DataTable GetInvList(string RecipeName)
        {
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            return db.ExecuteQueryCmd(str.SelectInvList(RecipeName));
        }

        internal bool SelectInv(string InvName)
        {
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            if (db.ExecuteQueryCmd(str.GetInv(InvName)).Rows.Count > 0)
                return true;
            else
                return false;
        }

        internal void UpdateInvName(string Invstring, string ExistingStr)
        {
            DatabaseStr str = new DatabaseStr();
            DatabaseFunction db = new DatabaseFunction();
            db.ExecuteNonQuery(str.UpdateInvF(Invstring, ExistingStr));
        }
    }

    internal class DatabaseStr
    {
        static public string connectingstr =
                                        "user id=sa;" +
                                        "password=Password123; Server=(local)\\SQLEXPRESS;" +
                                        "Trusted_Connection=yes;" +
                                        "database=StripMap; " +
                                        "connection timeout=10";

        public string InsertRecipeName(string RecipeName, string Blocks, string row, string col, string userid, string yield)
        {
            string str = "INSERT INTO [StripMap].[dbo].[RecipeList]([Datetime],[RecipeName],[Blocks],[bRow],[bCol],[UserID],[yield])VALUES(GETDATE(),"
                    + "'" + RecipeName + "'," + Blocks + "," + row + ","
                + col + ",'" + userid + "'" + "," + yield + ")";
            return str;
        }

        public string InsertLFInv(string RecipeName, string LFInv, string userid)
        {
            string str = "INSERT INTO [StripMap].[dbo].[LFInvList]([Datetime],[RecipeName],[LFInvList],[UserID])VALUES(GETDATE(),"
                    + "'" + RecipeName + "','" + LFInv + "','" + userid + "')";
            return str;
        }

        public string RemoveRecipe(string RecipeName)
        {
            return "DELETE FROM [StripMap].[dbo].[RecipeList] WHERE RecipeName= '" + RecipeName + "'";
        }

        public string RemoveInvF(string InvF)
        {
            return "DELETE FROM [StripMap].[dbo].[LFInvList] WHERE LFInvList= '" + InvF + "'";
        }

        public string SelectRecipeName(string RecipeName)
        {
            return "SELECT [Blocks],[bRow],[bCol],[yield] FROM [StripMap].[dbo].[RecipeList] WHERE RecipeName = '" + RecipeName + "'";
        }

        public string SelectInvList(string RecipeName)
        {
            return "SELECT [LFInvList] FROM [StripMap].[dbo].[LFInvList] WHERE RecipeName = '" + RecipeName + "'";
        }

        internal string GetRecipeList()
        {
            return "SELECT [RecipeName] FROM [StripMap].[dbo].[RecipeList] ORDER BY RecipeName DESC";
        }

        internal string GetInv(string InvName)
        {
            return "SELECT [LFInvList] FROM [StripMap].[dbo].[LFInvList] WHERE LFInvList = '" + InvName + "'";
        }

        internal string GetRecipeFromLFInv(string LFInv)
        {
            return "SELECT [RecipeName] FROM [StripMap].[dbo].[LFInvList] WHERE LFInvList = '" + LFInv + "'";
        }

        internal string UpdateInvF(string Invstring, string ExistingStr)
        {
            return "UPDATE [StripMap].[dbo].[LFInvList] SET [LFInvList] = '" + Invstring + "' WHERE LFInvList = '" + ExistingStr + "'";
        }
    }

    internal class DatabaseFunction
    {
        public delegate string inserlistfunction(string type, string sn, int indx, string rst);

        public void ExecuteNonQuery_listitem(string SN, string type, List<string> list, inserlistfunction cmdfunction)
        {
            DatabaseStr dbstr = new DatabaseStr();
            int indx = 0;
            using (SqlConnection connection = new SqlConnection(DatabaseStr.connectingstr))
            {
                try
                {
                    connection.Open();
                    using (SqlTransaction trans = connection.BeginTransaction())
                    {
                        using (SqlCommand command = new SqlCommand("", connection, trans))
                        {
                            foreach (string rst in list)
                            {
                                string cmdstring = cmdfunction(type, SN, indx, (string)list[indx]);
                                command.CommandText = cmdstring;
                                command.ExecuteNonQuery();
                                indx++;
                            }
                        }
                        trans.Commit();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally { connection.Close(); }
            }
        }

        public void ExecuteNonQuery(string cmdstring)
        {
            DatabaseStr dbstr = new DatabaseStr();
            using (SqlConnection connection = new SqlConnection(DatabaseStr.connectingstr))
            {
                try
                {
                    SqlCommand command = new SqlCommand(cmdstring, connection);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    //logfile
                    MessageBox.Show(ex.ToString());
                    throw ex;
                }
                finally { connection.Close(); }
            }
        }

        public DataTable ExecuteQueryCmd(string querystring)
        {
            DatabaseStr dbstr = new DatabaseStr();
            using (SqlConnection connection = new SqlConnection(DatabaseStr.connectingstr))
            {
                try
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(querystring, connection);
                    SqlDataReader rd = cmd.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load(rd);
                    return dt;
                }
                catch (Exception ex)
                { }
                finally { connection.Close(); }
            }
            return null;
        }
    }
}