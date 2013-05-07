using System;
using System.Data;
using System.Linq;
using System.Text;
using Sharp.Data.Databases;
using Sharp.Data.Filters;
using Sharp.Data.Query;
using Sharp.Data.Schema;
using Sharp.Util;

namespace Sharp.Data {
    public abstract class Dialect {
        public virtual string WordNull {
            get { return "NULL"; }
        }

		public virtual string WordNotNull {
            get { return "NOT NULL"; }
        }

		public virtual string WordWhere {
            get { return "WHERE"; }
        }

        public abstract string ParameterPrefix { get; }

        public string GetDialectName() {
            return GetType().Name;
        }

        public abstract DbType GetDbType(string sqlType, int dataPrecision);

        public abstract string[] GetCreateTableSqls(Table table);

        public abstract string[] GetDropTableSqls(string tableName);

        public abstract string GetPrimaryKeySql(string pkName, string table, params string[] columnNames);

        public abstract string GetForeignKeySql(string fkName, string table, string column, string referencingTable,
                                                string referencingColumn, OnDelete onDelete);

    	public virtual string GetDropForeignKeySql(string fkName, string tableName) {
    		return String.Format("alter table {0} drop constraint {1}", tableName, fkName);
    	}

    	public virtual string GetCreateIndexSql(string indexName, string table, params string[] columnNames) {
			return String.Format("create index {0} on {1} ({2})", indexName, table, StringHelper.Implode(columnNames, ",")); 		
    	}

    	public virtual string GetDropIndexSql(string indexName, string table) {
            return String.Format("drop index {0} on {1}", indexName, table);
    	}

    	public abstract string GetUniqueKeySql(string ukName, string table, params string[] columnNames);

        public abstract string GetDropUniqueKeySql(string uniqueKeyName, string tableName);

        public virtual string GetAddColumnSql(string table, Column column) {
            return String.Format("alter table {0} add {1}", table, GetColumnToSqlWhenCreate(column));
        }

        public virtual string[] GetDropColumnSql(string table, string columnName) {
            return new[] {String.Format("alter table {0} drop column {1}", table, columnName)};
        }

        public virtual string GetInsertSql(string table, string[] columns, object[] values) {
			if(values == null) {
				values = new object[columns.Length];
			}

            if (columns.Length != values.Length) {
                throw new ArgumentException("Columns and values length must ge the same!");
            }

            var sb = new StringBuilder();
            sb.Append("insert into ").Append(table).AppendLine(" (");
            for (int i = 0; i < columns.Length; i++) {
                sb.Append(columns[i]);
                if (i != columns.Length - 1) {
                    sb.AppendLine(",");
                }
            }

            sb.AppendLine(") values (");

            for (int i = 0; i < values.Length; i++) {
                sb.Append(GetParameterName(i)); //use parameters
                if (i != columns.Length - 1) {
                    sb.AppendLine(",");
                }
            }
            sb.AppendLine(")");
            return sb.ToString();
        }

        public abstract string GetInsertReturningColumnSql(string table, string[] columns, object[] values,
                                                           string returningColumnName, string returningParameterName);

        public virtual string GetUpdateSql(string table, string[] columns, object[] values) {
            if (columns.Length != values.Length) {
                throw new ArgumentException("Columns and values length must be the same!");
            }

            var sb = new StringBuilder();
            sb.Append("update ").Append(table).Append(" set ");

            for (int i = 0; i < columns.Length; i++) {
                sb.AppendFormat("{0} = {1}", columns[i], GetParameterName(i)); //use parameters
                
                if (i != columns.Length - 1) {
                    sb.Append(",");
                }
            }
            return sb.ToString();
        }

        public virtual string GetDeleteSql(string table) {
            return "delete from " + table;
        }

        public virtual string GetSelectSql(string[] tables, string[] columns) {
            StringBuilder sb = new StringBuilder();
            sb.Append("select ");
            for (int i = 0; i < columns.Length; i++) {
                sb.Append(columns[i]);
                if (i != columns.Length - 1) {
                    sb.Append(" ,");
                }
            }
            sb.Append(" from ");
            for (int i = 0; i < tables.Length; i++) {
                sb.Append(tables[i]);
                if (i != tables.Length - 1) {
                    sb.Append(" ,");
                }
            }            
            return sb.ToString();
        }

    	public abstract string WrapSelectSqlWithPagination(string sql, int skipRows, int numberOfRows);

        public virtual string GetWhereSql(Filter filter, int parameterStartIndex) {
            var whereBuilder = new WhereBuilder(this, parameterStartIndex);
            return whereBuilder.Build(filter);
        }

        public virtual string GetParameterName(int order) {
            return String.Format("{0}par{1}", ParameterPrefix, order);
        }

        public In[] ConvertToNamedParameters(object[] values) {
            return ConvertToNamedParameters(0, values);
        }

        public In[] ConvertToNamedParameters(int indexToStart, object[] values) {
            var pars = new In[values.Length];
            for (int i = 0; i < values.Length; i++) {
                pars[i] = new In {Name = GetParameterName(i + indexToStart), Value = values[i]};
            }
            return pars;
        }

        protected abstract string GetDbTypeString(DbType type, int precision);

        public abstract string GetColumnToSqlWhenCreate(Column col);

        public abstract string GetColumnValueToSql(object value);

        public virtual string GetLogicOperator(LogicOperator op) {
            switch (op) {
                case LogicOperator.And:
                    return "and";
                case LogicOperator.Or:
                    return "or";
            }
            return null;
        }

        protected virtual string GetWhereOperatorCompare(CompareOperator op) {
            switch (op) {
                case CompareOperator.Equals:
                    return "=";
                case CompareOperator.NotEquals:
                    return "<>";
                case CompareOperator.GreaterOrEqualThan:
                    return ">=";
                case CompareOperator.GreaterThan:
                    return ">";
                case CompareOperator.LessOrEqualThan:
                    return "<=";
                case CompareOperator.LessThan:
                    return "<";
            }
            return null;
        }

    	public abstract string GetTableExistsSql(string tableName);

    	public virtual string GetCountSql(string tableName) {
    		return "SELECT COUNT(*) FROM " + tableName;
    	}

		public virtual string GetOrderBySql(params OrderBy[] orderBy) {
			string[] orderByString = orderBy.Select(p => p.ColumnName + " " + GetOrderByWord(p.Direction)).ToArray();
			return "order by " + StringHelper.Implode(orderByString, ",");
		}

		public virtual string GetOrderByWord(OrderByDirection orderByDirection) {
			return orderByDirection == OrderByDirection.Descending ? "DESC" : "";
		}

        public string GetAddCommentToColumnSql(string tableName, string columnName, string comment) {
            return String.Format("COMMENT ON COLUMN {0}.{1} IS '{2}'", tableName, columnName, comment);
        }

        public string GetAddCommentToTableSql(string tableName, string comment) {
            return String.Format("COMMENT ON TABLE {0} IS '{1}'", tableName, comment);
        }
    }
}