﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using TenmoClient;
using TenmoServer.Models;
using TenmoServer.Security;

namespace TenmoServer.DAO
{
    public class TransferDAO : ITransferDAO
    {
        private IAccountDAO accountDAO;

        public TransferDAO(IAccountDAO accountDAO)
        {
            this.accountDAO = accountDAO;
        }

        private string connString = "Server=.\\SQLEXPRESS;Database=tenmo;Trusted_Connection=True;";

        //TODO: In SendTEBucks set up an if/else where in the foreach loop, if the userId == currentuserID, do not console writeline
        public List<User> GetAllUsers()
        {
            List<User> users = new List<User>();
            string sql = "Select user_id, username from users";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        User user = new User();
                        user.UserId = Convert.ToInt32(rdr["user_id"]);
                        user.Username = Convert.ToString(rdr["username"]);
                        users.Add(user);
                    }
                    return users;
                }
            }
            catch (SqlException ex)
            {
                throw;
            }
        }

        public Transfer TransferApprovedRequest(Transfer transfer)
        {
            //Get current User id balance
            decimal currentUserBalance = accountDAO.GetAccount(transfer.AccountFrom).Balance;

            //Get toUserid balance
            decimal toUserBalance = accountDAO.GetAccount(transfer.AccountTo).Balance;

            //Check balance of current user against sentMoney
            if (currentUserBalance >= transfer.Amount)
            {
                //Updated balance for user
                decimal newCurrentUserBalance = currentUserBalance - transfer.Amount;

                //Updated balance for toUserId
                decimal newToUserBalance = toUserBalance + transfer.Amount;

                //Update transfer status
                transfer.TransferStatusId = 2;

                //Update balance for current user
                UpdateBalance(transfer.AccountFrom, newCurrentUserBalance);

                //Update balance for toUser
                UpdateBalance(transfer.AccountTo, newToUserBalance);

                return transfer;
            }
            else
            {
                transfer.TransferStatusId = 3;
                return transfer;
            }
        }

        public Transfer SendMoneyTo(Transfer transfer) //removed fromUserId
        {
            //Get current User id balance
            decimal currentUserBalance = accountDAO.GetAccount(transfer.AccountFrom).Balance;

            //Get toUserid balance
            decimal toUserBalance = accountDAO.GetAccount(transfer.AccountTo).Balance;

            //Check balance of current user against sentMoney
            if (currentUserBalance >= transfer.Amount)
            {
                //Updated balance for user
                decimal newCurrentUserBalance = currentUserBalance - transfer.Amount;

                //Updated balance for toUserId
                decimal newToUserBalance = toUserBalance + transfer.Amount;

                //Update transfer status
                transfer.TransferStatusId = 2;

                //Update balance for current user
                UpdateBalance(transfer.AccountFrom, newCurrentUserBalance);

                //Update balance for toUser
                UpdateBalance(transfer.AccountTo, newToUserBalance);

                LogTransfer(transfer.AccountFrom, transfer.AccountTo, transfer.Amount, 2, transfer.TransferStatusId);

                return transfer;
            }
            else
            {
                transfer.TransferStatusId = 3;
                return transfer;
            }
        }

        public Transfer RequestMoney(Transfer transfer)
        {
            LogTransfer(transfer.AccountFrom, transfer.AccountTo, transfer.Amount, 1, 1);
            return transfer;
        }

        public bool UpdateBalance(int userId, decimal newBalance)
        {
            string sql = @"Update accounts
                               Set balance = @newBalance
                                   Where user_id = @userId;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@newBalance", newBalance);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public bool LogTransfer(int fromId, int toId, decimal sentMoney, int transferType, int transferStatusId)
        {
            string sql = @"Insert into transfers (transfer_type_id, transfer_status_id, account_from, account_to, amount)
                            Values(@transferTypeId, @transferStatusId, @accountFrom, @accountTo, @amount)";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@transferTypeId", transferType);
                    cmd.Parameters.AddWithValue("@transferStatusId", transferStatusId);

                    if (transferType == 2)
                    {
                        cmd.Parameters.AddWithValue("@accountFrom", fromId);
                        cmd.Parameters.AddWithValue("@accountTo", toId);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@accountFrom", toId);
                        cmd.Parameters.AddWithValue("@accountTo", fromId);
                    }

                    cmd.Parameters.AddWithValue("@amount", sentMoney);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public Transfer UpdateTransfer(Transfer transfer)
        {
            string sql = @"Update transfers
                                Set transfers.transfer_status_id = @transferStatus,
                                transfers.transfer_type_id = @transferType,
                                transfers.account_from = @accountFrom,
                                transfers.account_to = @accountTo,
                                transfers.amount = @amount
                                        Where transfer_id = @transferId;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@transferId", transfer.TransferId);
                    cmd.Parameters.AddWithValue("@transferStatus", transfer.TransferStatusId);
                    cmd.Parameters.AddWithValue("@transferType", transfer.TransferTypeId);
                    cmd.Parameters.AddWithValue("@accountFrom", transfer.AccountFrom);
                    cmd.Parameters.AddWithValue("@accountTo", transfer.AccountTo);
                    cmd.Parameters.AddWithValue("@amount", transfer.Amount);

                    int rowsAffected = cmd.ExecuteNonQuery();
                }
                return transfer;
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        //public Transfer UpdateRejectedTransfer(int transferId)
        //{
        //    string sql = @"Update transfers
        //                        Set transfers.transfer_status_id = 3
        //                                Where transfer_id = @transferId";
        //    Transfer tran = new Transfer();
        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(connString))
        //        {
        //            conn.Open();

        //            SqlCommand cmd = new SqlCommand(sql, conn);
        //            cmd.Parameters.AddWithValue("@transferId", transferId);

        //            int rowsAffected = cmd.ExecuteNonQuery();
        //            SqlDataReader rdr = cmd.ExecuteReader();
        //            while (rdr.Read())
        //            {
        //                tran.TransferId = Convert.ToInt32(rdr["transfer_id"]);
        //                tran.TransferTypeId = Convert.ToInt32(rdr["transfer_type_id"]);
        //                tran.TransferStatusId = Convert.ToInt32(rdr["transfer_status_id"]);
        //                tran.AccountFrom = Convert.ToInt32(rdr["account_from"]);
        //                tran.AccountTo = Convert.ToInt32(rdr["account_to"]);
        //                tran.Amount = Convert.ToDecimal(rdr["amount"]);
        //            }
        //        }
        //        return tran;
        //    }
        //    catch (SqlException ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }
        //}

        public TransferDetails GetTransferDetails(int transferId)
        {
            string sql = @"Select transfers.*, users.username as usernameFrom,
                        (Select username from users where transfers.account_to = users.user_id) as usernameTo from transfers
                            join accounts on accounts.user_id = transfers.account_from
                                join users on accounts.user_id = users.user_id
                                    where transfers.transfer_id = @transferId";
            Transfer tran = new Transfer();
            TransferDetails details = new TransferDetails();

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@transferId", transferId);

                    SqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        tran.TransferId = Convert.ToInt32(rdr["transfer_id"]);
                        tran.TransferTypeId = Convert.ToInt32(rdr["transfer_type_id"]);
                        tran.TransferStatusId = Convert.ToInt32(rdr["transfer_status_id"]);
                        details.From = Convert.ToString(rdr["usernameFrom"]);
                        details.To = Convert.ToString(rdr["usernameTo"]);
                        tran.Amount = Convert.ToDecimal(rdr["amount"]);
                    }

                    details.Id = tran.TransferId;
                    details.Amount = tran.Amount;

                    if (tran.TransferTypeId == 1)
                    {
                        details.Type = "Request";
                    }
                    else
                    {
                        details.Type = "Send";
                    }
                    if (tran.TransferStatusId == 1)
                    {
                        details.Status = "Pending";
                    }
                    else if (tran.TransferStatusId == 2)
                    {
                        details.Status = "Approved";
                    }
                    else
                    {
                        details.Status = "Rejected";
                    }

                    return details;
                }
            }
            catch (SqlException ex)
            {
                throw;
            }
        }

        //Get all transfers

        //If userId in Account_from == currentUser - pull it from list
        //Use getUsers method to determine nonCurrentUsername and add it as to:

        //If userId in Account_to == currentUser - pull it from list
        //Use getUsers method to determine noncurrentusername and add it as from:

        public List<Transfer> GetUserTransfers()
        {
            string sql = @"Select transfers.*, users.username as usernameFrom,
                        (Select username from users where transfers.account_to = users.user_id) as usernameTo from transfers
                            join accounts on accounts.user_id = transfers.account_from
                                join users on accounts.user_id = users.user_id
                                   order by transfer_id";

            List<Transfer> transfers = new List<Transfer>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    SqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        Transfer tran = new Transfer();

                        tran.TransferId = Convert.ToInt32(rdr["transfer_id"]);
                        tran.TransferTypeId = Convert.ToInt32(rdr["transfer_type_id"]);
                        tran.TransferStatusId = Convert.ToInt32(rdr["transfer_status_id"]);
                        tran.AccountFrom = Convert.ToInt32(rdr["account_from"]);
                        tran.AccountTo = Convert.ToInt32(rdr["account_to"]);
                        tran.Amount = Convert.ToDecimal(rdr["amount"]);
                        tran.UsernameFrom = Convert.ToString(rdr["usernameFrom"]);
                        tran.UsernameTo = Convert.ToString(rdr["usernameTo"]);

                        transfers.Add(tran);
                    }
                }

                return transfers;
            }
            catch (SqlException ex)
            {
                throw;
            }
        }

        //public List<List<Transfer>> GetUserTransfers()
        //{
        //    string sqlFrom = @"Select transfers.*, users.username as usernameFrom from transfers
        //                    join accounts on accounts.user_id = transfers.account_from
        //                        join users on accounts.user_id = users.user_id
        //                            where 1 in (account_from, account_to);";

        //    string sqlTo = @"Select transfers.*, users.username as usernameTo from transfers
        //                        join accounts on accounts.user_id = transfers.account_to
        //                        join users on accounts.user_id = users.user_id ";

        //    List<Transfer> transfersTo = new List<Transfer>();
        //    List<Transfer> transfersFrom = new List<Transfer>();
        //    List<List<Transfer>> transfersJoined = new List<List<Transfer>>();

        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(connString))
        //        {
        //            conn.Open();

        //            SqlCommand cmd = new SqlCommand(sqlFrom, conn);

        //            SqlDataReader rdr = cmd.ExecuteReader();

        //            while (rdr.Read())
        //            {
        //                Transfer tran = new Transfer();

        //                tran.TransferId = Convert.ToInt32(rdr["transfer_id"]);
        //                tran.TransferTypeId = Convert.ToInt32(rdr["transfer_type_id"]);
        //                tran.TransferStatusId = Convert.ToInt32(rdr["transfer_status_id"]);
        //                tran.AccountFrom = Convert.ToInt32(rdr["account_from"]);
        //                tran.AccountTo = Convert.ToInt32(rdr["account_to"]);
        //                tran.Amount = Convert.ToDecimal(rdr["amount"]);
        //                tran.UsernameFrom = Convert.ToString(rdr["usernameFrom"]);

        //                transfersFrom.Add(tran);
        //            }
        //        }
        //        using (SqlConnection conn = new SqlConnection(connString))
        //        {
        //            conn.Open();

        //            SqlCommand cmd = new SqlCommand(sqlTo, conn);

        //            SqlDataReader rdr = cmd.ExecuteReader();

        //            while (rdr.Read())
        //            {
        //                Transfer tran = new Transfer();

        //                tran.TransferId = Convert.ToInt32(rdr["transfer_id"]);
        //                tran.TransferTypeId = Convert.ToInt32(rdr["transfer_type_id"]);
        //                tran.TransferStatusId = Convert.ToInt32(rdr["transfer_status_id"]);
        //                tran.AccountFrom = Convert.ToInt32(rdr["account_from"]);
        //                tran.AccountTo = Convert.ToInt32(rdr["account_to"]);
        //                tran.Amount = Convert.ToDecimal(rdr["amount"]);
        //                tran.UsernameTo = Convert.ToString(rdr["usernameTo"]);

        //                transfersTo.Add(tran);
        //            }
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        throw;
        //    }
        //    transfersJoined.Add(transfersTo);
        //    transfersJoined.Add(transfersFrom);
        //    return transfersJoined;
        //}
    }
}