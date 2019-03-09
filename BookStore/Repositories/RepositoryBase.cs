﻿using System;
using BookStore.Domain.Models;
using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;

namespace BookStore.Repositories
{
    public abstract class RepositoryBase<T> : IRepository<T> where T : Entity
    {
        protected DbConnection Connection { get; }

        protected string TableName { get; }

        public RepositoryBase(DbConnection connection)
        {
            Connection = connection;
            TableName = typeof(T).Name;
        }

        public virtual IEnumerable<T> All()
        {
            return Connection.Query<T>($"SELECT * FROM {TableName}");
        }

        public virtual bool Delete(int id)
        {
            try
            {
                var affectedRows = Connection.Execute($"DELETE FROM {TableName} WHERE Id=@Id", 
                    new {Id = id});

                return affectedRows > 0;
            }
            catch (DbException)
            {
                return false;
            }
        }

        public virtual T Find(int id)
        {
            return Connection.QueryFirstOrDefault<T>($"SELECT * FROM {TableName} WHERE Id=@Id",
                new {Id = id});
        }

        public abstract T Create(T item);

        public abstract bool Update(T item);

        protected virtual IEnumerable<T> AllWith<TForeign>(Action<T, TForeign> associateAction)
            where TForeign : Entity
        {
            var primaryTableName = typeof(TForeign).Name;
            return Connection.Query<T, TForeign, T>(
            $"SELECT * FROM {TableName} LEFT JOIN {primaryTableName} " +
                $"ON {TableName}.{primaryTableName}Id = {primaryTableName}.Id",
                (item, primary) =>
                {
                    associateAction(item, primary);
                    return item;
                });
        }

        protected virtual IEnumerable<T> AllWith<TForeign>(Expression<Func<T, object>> propertySelector)
            where TForeign : Entity
        {
            var primaryTableName = typeof(TForeign).Name;
            var expression = propertySelector.Body as MemberExpression;
            var propertyName = expression.Member.Name;
            return Connection.Query<T, TForeign, T>(
                $"SELECT * FROM {TableName} LEFT JOIN {primaryTableName} " +
                $"ON {TableName}.{primaryTableName}Id = {primaryTableName}.Id",
                (item, primary) =>
                {
                    typeof(T).GetProperty(propertyName).SetValue(item, primary);
                    return item;
                });
        }
    }
}