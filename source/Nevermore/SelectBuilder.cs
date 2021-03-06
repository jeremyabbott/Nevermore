﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.AST;

namespace Nevermore
{
    public class JoinSelectBuilder : SelectBuilderBase<JoinedSource>
    {
        public JoinSelectBuilder(JoinedSource @from) : this(@from, new List<IWhereClause>(), new List<OrderByField>())
        {
        }

        JoinSelectBuilder(JoinedSource @from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses) 
            : base(@from, whereClauses, orderByClauses)
        {
        }

        JoinSelectBuilder(JoinedSource @from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection, IRowSelection rowSelection, bool shouldIgnoreDefaultOrderBy) 
            : base(@from, whereClauses, orderByClauses, columnSelection, rowSelection, shouldIgnoreDefaultOrderBy)
        {
        }

        protected override ISelectColumns DefaultSelect => new SelectAllFrom(From.Source.Alias);

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new TableColumn(new Column("Id"), From.Source.Alias));
        }

        public override ISelectBuilder Clone()
        {
            return new JoinSelectBuilder(From, new List<IWhereClause>(WhereClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection, ShouldIgnoreDefaultOrderBy);
        }

        public override void AddWhere(UnaryWhereParameter whereParams)
        {
            WhereClauses.Add(new UnaryWhereClause(new AliasedWhereFieldReference(From.Source.Alias, new WhereFieldReference(whereParams.FieldName)), 
                whereParams.Operand, whereParams.ParameterName));
        }

        public override void AddWhere(BinaryWhereParameter whereParams)
        {
            WhereClauses.Add(new BinaryWhereClause(new AliasedWhereFieldReference(From.Source.Alias, new WhereFieldReference(whereParams.FieldName)), 
                whereParams.Operand, whereParams.FirstParameterName, whereParams.SecondParameterName));
        }

        public override void AddWhere(ArrayWhereParameter whereParams)
        {
            WhereClauses.Add(new ArrayWhereClause(new AliasedWhereFieldReference(From.Source.Alias, new WhereFieldReference(whereParams.FieldName)), 
                whereParams.Operand, whereParams.ParameterNames));
        }

        public override void AddOrder(string fieldName, bool @descending)
        {
            OrderByClauses.Add(new OrderByField(new TableColumn(new Column(fieldName), From.Source.Alias), @descending ? OrderByDirection.Descending : OrderByDirection.Ascending));
        }

        public override void AddColumn(string columnName)
        {
            AddColumnSelection(new TableColumn(new Column(columnName), From.Source.Alias));
        }

        public override void AddColumn(string columnName, string columnAlias)
        {
            AddColumnSelection(new AliasedColumn(new TableColumn(new Column(columnName), From.Source.Alias), columnAlias));
        }

        public override void AddRowNumberColumn(string alias, IReadOnlyList<Column> partitionBys)
        {
            InnerAddRowNumberColumn(alias, partitionBys.Select(c => new TableColumn(c, From.Source.Alias)).ToList());
        }
    }

    public class TableSelectBuilder : SelectBuilderBase<ITableSource>
    {
        public TableSelectBuilder(ITableSource @from) 
            : this(@from, new List<IWhereClause>(), new List<OrderByField>())
        {
        }

        TableSelectBuilder(ITableSource from, List<IWhereClause> whereClauses,
            List<OrderByField> orderByClauses)
            : base(from, whereClauses, orderByClauses)
        {
        }

        TableSelectBuilder(ITableSource from, List<IWhereClause> whereClauses,
            List<OrderByField> orderByClauses, ISelectColumns columnSelection, 
            IRowSelection rowSelection, bool shouldIgnoreDefaultOrderBy)
            : base(from, whereClauses, orderByClauses, columnSelection, rowSelection, shouldIgnoreDefaultOrderBy)
        {
        }

        protected override ISelectColumns DefaultSelect => new SelectAllSource();

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new Column("Id"));
        }

        public override ISelectBuilder Clone()
        {
            return new TableSelectBuilder(From, new List<IWhereClause>(WhereClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection, ShouldIgnoreDefaultOrderBy);
        }
    }

    public class SubquerySelectBuilder : SelectBuilderBase<ISubquerySource>
    {
        public SubquerySelectBuilder(ISubquerySource @from) 
            : this(@from, new List<IWhereClause>(), new List<OrderByField>())
        {
        }

        SubquerySelectBuilder(ISubquerySource @from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses) 
            : base(@from, whereClauses, orderByClauses)
        {
        }

        SubquerySelectBuilder(ISubquerySource @from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection, IRowSelection rowSelection, bool shouldIgnoreDefaultOrderBy) 
            : base(@from, whereClauses, orderByClauses, columnSelection, rowSelection, shouldIgnoreDefaultOrderBy)
        {
        }

        protected override ISelectColumns DefaultSelect => new SelectAllSource();

        protected override IEnumerable<OrderByField> GetDefaultOrderByFields()
        {
            yield return new OrderByField(new TableColumn(new Column("Id"), From.Alias));
        }

        public override ISelectBuilder Clone()
        {
            return new SubquerySelectBuilder(From, new List<IWhereClause>(WhereClauses), new List<OrderByField>(OrderByClauses), ColumnSelection, RowSelection, ShouldIgnoreDefaultOrderBy);
        }
    }

    public abstract class SelectBuilderBase<TSource> : ISelectBuilder where TSource : ISelectSource
    {
        protected readonly TSource From;
        protected readonly List<OrderByField> OrderByClauses;
        protected readonly List<IWhereClause> WhereClauses;
        protected ISelectColumns ColumnSelection;
        protected IRowSelection RowSelection;
        protected bool ShouldIgnoreDefaultOrderBy;

        protected SelectBuilderBase(TSource from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses)
            :this(from, whereClauses, orderByClauses, null, new AllRows(), false)
        {
        }

        protected SelectBuilderBase(TSource from, List<IWhereClause> whereClauses, List<OrderByField> orderByClauses, 
            ISelectColumns columnSelection,
            IRowSelection rowSelection, 
            bool shouldIgnoreDefaultOrderBy)
        {
            From = from;
            WhereClauses = whereClauses;
            OrderByClauses = orderByClauses;
            this.RowSelection = rowSelection;
            this.ColumnSelection = columnSelection;
            this.ShouldIgnoreDefaultOrderBy = shouldIgnoreDefaultOrderBy;
        }

        protected abstract ISelectColumns DefaultSelect { get; }

        protected abstract IEnumerable<OrderByField> GetDefaultOrderByFields();

        public ISelect GenerateSelect()
        {
            return new Select(RowSelection, GetColumnSelection(), From, GetWhere() ?? new Where(), GetOrderBy());
        }

        public abstract ISelectBuilder Clone();

        Where GetWhere()
        {
            return WhereClauses.Any() ? new Where(new AndClause(WhereClauses)) : null;
        }

        OrderBy GetOrderBy()
        {
            // If you are doing something like COUNT(*) then it doesn't make sense to include an Order By clause
            if (GetColumnSelection().AggregatesRows)
            {
                return null;
            }

            if (OrderByClauses.Any()) return new OrderBy(OrderByClauses);

            if (ShouldIgnoreDefaultOrderBy) return null;
            var orderByFields = GetDefaultOrderByFields().ToList();
            return !orderByFields.Any() ? null : new OrderBy(orderByFields);

        }

        public void AddTop(int top)
        {
            RowSelection = new Top(top);
        }

        public virtual void AddOrder(string fieldName, bool @descending)
        {
            OrderByClauses.Add(new OrderByField(new Column(fieldName), @descending ? OrderByDirection.Descending : OrderByDirection.Ascending));
        }

        public void IgnoreDefaultOrderBy()
        {
            ShouldIgnoreDefaultOrderBy = true;
        }

        public virtual void AddWhere(UnaryWhereParameter whereParams)
        {
            WhereClauses.Add(new UnaryWhereClause(new WhereFieldReference(whereParams.FieldName), whereParams.Operand, whereParams.ParameterName));
        }

        public virtual void AddWhere(BinaryWhereParameter whereParams)
        {
            WhereClauses.Add(new BinaryWhereClause(new WhereFieldReference(whereParams.FieldName), whereParams.Operand, 
                whereParams.FirstParameterName, whereParams.SecondParameterName));
        }

        public virtual void AddWhere(ArrayWhereParameter whereParams)
        {
            WhereClauses.Add(new ArrayWhereClause(new WhereFieldReference(whereParams.FieldName), whereParams.Operand, whereParams.ParameterNames));
        }

        public void AddWhere(string whereClause)
        {
            WhereClauses.Add(new CustomWhereClause(whereClause));
        }

        public virtual void AddColumn(string columnName)
        {
            AddColumnSelection(new Column(columnName));
        }

        public virtual void AddColumn(string columnName, string columnAlias)
        {
            AddColumnSelection(new AliasedColumn(new Column(columnName), columnAlias));
        }

        public void AddColumnSelection(ISelectColumns columns)
        {
            ColumnSelection = ColumnSelection == null
                ? columns
                : new AggregateSelectColumns(new List<ISelectColumns>() { ColumnSelection, columns });
        }

        public virtual void AddRowNumberColumn(string alias, IReadOnlyList<Column> partitionBys)
        {
            InnerAddRowNumberColumn(alias, partitionBys);
        }

        public void AddRowNumberColumn(string alias, IReadOnlyList<TableColumn> partitionBys)
        {
            InnerAddRowNumberColumn(alias, partitionBys);
        }

        protected void InnerAddRowNumberColumn(string alias, IReadOnlyList<IColumn> partitionBys)
        {
            var orderByClauses = OrderByClauses.Any() 
                ? OrderByClauses 
                : ShouldIgnoreDefaultOrderBy ? new List<OrderByField>() : GetDefaultOrderByFields().ToList();
            if (!orderByClauses.Any())
                throw new InvalidOperationException("Cannot create a ROW_NUMBER() column without an order by clause");

            var partitionBy = partitionBys.Any() ? new PartitionBy(partitionBys) : null;
            AddColumnSelection(new SelectRowNumber(new Over(new OrderBy(orderByClauses.ToList()), partitionBy), alias));
            OrderByClauses.Clear();
        }

        public void AddDefaultColumnSelection()
        {
            AddColumnSelection(DefaultSelect);
        }

        ISelectColumns GetColumnSelection() => ColumnSelection ?? DefaultSelect;
    }
}