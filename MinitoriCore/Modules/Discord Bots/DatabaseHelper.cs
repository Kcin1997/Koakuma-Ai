using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using static MinitoriCore.AccountGateService;

namespace MinitoriCore
{
    public class DatabaseHelper
    {
        private string ConnectionString;

        public async Task Install(IServiceProvider _services)
        {
            ConnectionString = _services.GetService<Config>().DatabaseConnectionString;
        }

        public async Task<LoggedUser> GetLoggedUser(ulong userId)
        {
            LoggedUser temp = null;

            using (SQLiteConnection db = new SQLiteConnection(ConnectionString))
            {
                await db.OpenAsync();

                using (var cmd = new SQLiteCommand("select * from users where UserId = @1;", db))
                {
                    cmd.Parameters.AddWithValue("@1", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            temp = new LoggedUser()
                            {
                                UserId = Convert.ToUInt64((string)reader["UserId"]),
                                ApprovedAccess = (long)reader["ApprovedAccess"] == 1 ? true : false,
                                NewAccount = (long)reader["NewAccount"] == 1 ? true : false,
                                ApprovalModId = reader["ApprovalModId"] == DBNull.Value ? 0 : Convert.ToUInt64((string)reader["ApprovalModId"]),
                                DenialReasons = (Filter)(int)reader["DenialReasons"], // no idea if this double cast does anything but hey
                                LogMessageId = Convert.ToUInt64((string)reader["LogMessage"]),
                                OriginalJoinTime = DateTimeOffset.Parse((string)reader["OriginalJoinTime"]),
                                JoinCount = (int)reader["JoinCount"]
                            };
                        }
                    }
                }

                db.Close();
            }

            return temp;
        }

        public async Task<Dictionary<ulong, LoggedUser>> GetAllusers()
        {
            Dictionary<ulong, LoggedUser> temp = new Dictionary<ulong, LoggedUser>();

            try
            {
                using (SQLiteConnection db = new SQLiteConnection(ConnectionString))
                {
                    await db.OpenAsync();

                    using (var cmd = new SQLiteCommand("select * from users;", db))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                temp.Add(Convert.ToUInt64((string)reader["UserId"]),
                                    new LoggedUser()
                                    {
                                        UserId = Convert.ToUInt64((string)reader["UserId"]),
                                        ApprovedAccess = (long)reader["ApprovedAccess"] == 1 ? true : false,
                                        NewAccount = (long)reader["NewAccount"] == 1 ? true : false,
                                        ApprovalModId = reader["ApprovalModId"] == DBNull.Value ? 0 : Convert.ToUInt64((string)reader["ApprovalModId"]),
                                        DenialReasons = (Filter)(int)reader["DenialReasons"], // no idea if this double cast does anything but hey
                                        LogMessageId = Convert.ToUInt64((string)reader["LogMessage"]),
                                        OriginalJoinTime = DateTimeOffset.Parse((string)reader["OriginalJoinTime"]),
                                        JoinCount = (int)reader["JoinCount"]
                                    });
                            }
                        }
                    }

                    db.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error bulk loading!\nMessage: {ex.Message}\nSource: {ex.Source}\n{ex.InnerException}");
                //System.Environment.Exit(0);
            }

            return temp;
        }

        public async Task<LoggedUser> AddLoggedUser(ulong userId, DateTimeOffset originalJoinTime, bool approvedAccess = false, bool newAccount = false, ulong? approvalModId = null, int denialReasons = 0, ulong? logMessageId = null)
        {
            LoggedUser temp = new LoggedUser() { UserId = userId, ApprovedAccess = approvedAccess, NewAccount = newAccount, ApprovalModId = approvalModId, DenialReasons = (Filter)denialReasons, LogMessageId = logMessageId, OriginalJoinTime = originalJoinTime, JoinCount = 1 };

            await BulkAddLoggedUser(new List<LoggedUser> { temp });

            return temp;
        }

        public async Task BulkAddLoggedUser(IEnumerable<LoggedUser> users)
        {
            try
            {
                using (SQLiteConnection db = new SQLiteConnection(ConnectionString))
                {
                    await db.OpenAsync();
                    using (var tr = db.BeginTransaction())
                    {
                        foreach (var u in users)
                        {
                            using (var cmd = new SQLiteCommand("insert into users (UserId, ApprovedAccess, NewAccount, ApprovalModId, DenialReasons, LogMessage, OriginalJoinTime, JoinCount)" +
                                " values (@1, @2, @3, @4, @5, @6, @7, @8);", db))
                            {
                                cmd.Parameters.AddWithValue("@1", u.UserId.ToString());
                                cmd.Parameters.AddWithValue("@2", u.ApprovedAccess ? 1 : 0); // to the me from the future: this is converting true/false into 1/0
                                cmd.Parameters.AddWithValue("@3", u.NewAccount ? 1 : 0);
                                cmd.Parameters.AddWithValue("@4", u.ApprovalModId.ToString() ?? null);
                                cmd.Parameters.AddWithValue("@5", u.DenialReasons);
                                cmd.Parameters.AddWithValue("@6", u.LogMessageId);
                                cmd.Parameters.AddWithValue("@7", u.OriginalJoinTime.ToString());
                                cmd.Parameters.AddWithValue("@8", u.JoinCount);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        tr.Commit();
                    }

                    db.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error bulk saving!\nMessage: {ex.Message}\nSource: {ex.Source}\n{ex.InnerException}");
            }
        }
    }
}
