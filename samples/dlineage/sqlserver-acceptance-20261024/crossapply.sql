SELECT C.CustomerID,
    O.SalesOrderID,
    O.TotalDue
FROM
    AdventureWorks.Sales.Customer AS C
CROSS APPLY
    AdventureWorks.dbo.fn_GetTopOrders(C.CustomerID, 3) AS O
ORDER BY
    CustomerID ASC, TotalDue DESC