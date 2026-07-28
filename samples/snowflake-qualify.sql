-- Snowflake QUALIFY, which filters on a window function without a subquery.
-- Parsing this as Oracle produces a syntax error, so it is a quick check that
-- the /t flag is really selecting the dialect.
--   dotnet run --project src/demos/checksyntax/demos.checksyntax.csproj -c Release -- /f samples/snowflake-qualify.sql /t snowflake

SELECT o.customer_id,
       o.order_id,
       o.order_total,
       ROW_NUMBER() OVER (PARTITION BY o.customer_id ORDER BY o.order_total DESC) AS rn
FROM   orders o
WHERE  o.order_date >= '2026-01-01'
QUALIFY rn <= 3;
