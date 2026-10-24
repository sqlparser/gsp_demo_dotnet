-- rank()
SELECT i.ProductID, p.Name, i.LocationID, i.Quantity, RANK() OVER (PARTITION BY i.LocationID order by i.Quantity) as RANK
FROM Production.ProductInventory i JOIN Production.Product p 
ON i.ProductID = p.ProductID
ORDER BY p.Name;

-- ntile()
SELECT c.FirstName, c.LastName, NTILE(4) OVER(ORDER BY SalesYTD DESC) AS 'Quartile', s.SalesYTD, a.PostalCode
From Sales.SalesPerson s JOIN Person.Contact c on s.SalesPersonID = c.ContactID
JOIN Person.Address a ON a.AddressID = c.ContactID
WHERE TerritoryID IS NOT NULL AND SalesYTD <> 0;

-- dense_rank()
SELECT  i.ProductID, p.Name, i.LocationID, i.Quantity, DENSE_RANK() OVER (PARTITION BY i.LocationID order by i.Quantity) as DENSE_RANK
FROM Production.ProductInventory i JOIN Production.Product p ON i.ProductID = p.ProductID
ORDER BY Name;

-- row_number()
SELECT c.FirstName, c.LastName, ROW_NUMBER() OVER(ORDER BY SalesYTD DESC) AS 'Row Number', s.SalesYTD, a.PostalCode
FROM Sales.SalesPerson s JOIN Person.Contact c on s.SalesPersonID = c.ContactID
JOIN Person.Address a ON a.AddressID = c.ContactID
WHERE TerritoryID IS NOT NULL AND SalesYTD <> 0

