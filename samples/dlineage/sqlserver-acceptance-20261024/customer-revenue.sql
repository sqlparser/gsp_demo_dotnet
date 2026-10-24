-- Column Lineage: trace each target column back to its sources.
-- T-SQL: bracketed identifiers, dbo schema, SELECT ... INTO, STRING_AGG, CONCAT, FORMAT.
SELECT
    c.[customer_id],
    CONCAT(c.[first_name], ' ', c.[last_name])           AS [customer_name],
    STRING_AGG(p.[product_name], ', ')                   AS [product_list],
    SUM(oi.[quantity] * p.[unit_price])                  AS [total_revenue],
    FORMAT(MAX(o.[order_date]), 'yyyy-MM-dd')            AS [last_order]
INTO [dbo].[customer_revenue]
FROM [dbo].[orders] o
JOIN [dbo].[order_items] oi ON oi.[order_id]   = o.[order_id]
JOIN [dbo].[products]    p  ON p.[product_id]  = oi.[product_id]
JOIN [dbo].[customers]   c  ON c.[customer_id] = o.[customer_id]
WHERE o.[status] = 'PAID'
GROUP BY c.[customer_id], c.[first_name], c.[last_name];
