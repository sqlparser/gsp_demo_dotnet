-- T-SQL: TOP, a CTE, and a windowed aggregate.
--   dotnet run --project src/demos/formatsql/demos.formatsql.csproj -c Release -- /f samples/mssql-report.sql /t mssql
--   dotnet run --project src/demos/gettablecolumns/demos.gettablecolumns.csproj -c Release -- /f samples/mssql-report.sql /t mssql

WITH regional_sales AS (
    SELECT s.region_id,
           s.sales_rep_id,
           SUM(s.amount) AS total_amount
    FROM   sales s
    WHERE  s.sale_date >= '2026-01-01'
    GROUP  BY s.region_id, s.sales_rep_id
)
SELECT TOP 10
       r.region_name,
       rep.full_name,
       rs.total_amount,
       rs.total_amount * 100.0 / SUM(rs.total_amount) OVER (PARTITION BY rs.region_id) AS pct_of_region
FROM   regional_sales rs
       INNER JOIN regions r   ON r.region_id = rs.region_id
       INNER JOIN sales_reps rep ON rep.sales_rep_id = rs.sales_rep_id
ORDER BY rs.total_amount DESC;
