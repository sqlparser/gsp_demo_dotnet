-- PostgreSQL: :: casts, a correlated subquery and a derived table. Columns here
-- are resolvable only by walking into the subqueries, which is what
-- extractTableColumns demonstrates.
--   dotnet run --project src/demos/extractTableColumns/demos.extractTableColumns.csproj -c Release -- /f samples/postgresql-nested.sql /t postgresql

SELECT c.customer_id,
       c.email::text                          AS email,
       recent.last_order_date,
       (SELECT COUNT(*)
        FROM   orders o2
        WHERE  o2.customer_id = c.customer_id
        AND    o2.status = 'shipped')         AS shipped_count
FROM   customers c
       JOIN (SELECT o.customer_id,
                    MAX(o.order_date)::date AS last_order_date
             FROM   orders o
             GROUP  BY o.customer_id) recent
         ON recent.customer_id = c.customer_id
WHERE  c.created_at >= NOW() - INTERVAL '1 year';
