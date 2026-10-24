USE AdventureWorks;
GO
IF OBJECT_ID (N'dbo.ISOweek', N'FN') IS NOT NULL
    DROP FUNCTION dbo.ISOweek;
GO
CREATE FUNCTION dbo.ISOweek (@DATE datetime)
RETURNS int
WITH EXECUTE AS CALLER
AS
BEGIN
     DECLARE @ISOweek int
     SET @ISOweek= DATEPART(wk,@DATE)+1
          -DATEPART(wk,CAST(DATEPART(yy,@DATE) as CHAR(4))+'0104')
--Special cases: Jan 1-3 may belong to the previous year
     IF (@ISOweek=0) 
          SET @ISOweek=dbo.ISOweek(CAST(DATEPART(yy,@DATE)-1 
               AS CHAR(4))+'12'+ CAST(24+DATEPART(DAY,@DATE) AS CHAR(2)))+1
--Special case: Dec 29-31 may belong to the next year
     IF ((DATEPART(mm,@DATE)=12) AND 
          ((DATEPART(dd,@DATE)-DATEPART(dw,@DATE))>= 28))
          SET @ISOweek=1
     RETURN(@ISOweek)
END;
GO

USE AdventureWorks;
GO
IF OBJECT_ID (N'Sales.fn_SalesByStore', N'IF') IS NOT NULL
    DROP FUNCTION Sales.fn_SalesByStore;
GO
CREATE FUNCTION Sales.fn_SalesByStore (@storeid int)
RETURNS TABLE
AS
RETURN 
(
    SELECT P.ProductID, P.Name, SUM(SD.LineTotal) AS 'YTD Total'
    FROM Production.Product AS P 
      JOIN Sales.SalesOrderDetail AS SD ON SD.ProductID = P.ProductID
      JOIN Sales.SalesOrderHeader AS SH ON SH.SalesOrderID = SD.SalesOrderID
    WHERE SH.CustomerID = @storeid
    GROUP BY P.ProductID, P.Name
); 
GO


USE AdventureWorks;
GO
IF OBJECT_ID (N'dbo.fn_FindReports', N'TF') IS NOT NULL
    DROP FUNCTION dbo.fn_FindReports;
GO
CREATE FUNCTION dbo.fn_FindReports (@InEmpID INTEGER)
RETURNS @retFindReports TABLE 
(
    EmployeeID int primary key NOT NULL,
    Name nvarchar(255) NOT NULL,
    Title nvarchar(50) NOT NULL,
    EmployeeLevel int NOT NULL,
    Sort nvarchar (255) NOT NULL
)
--Returns a result set that lists all the employees who report to the 
--specific employee directly or indirectly.*/
AS
BEGIN
   WITH DirectReports(Name, Title, EmployeeID, EmployeeLevel, Sort) AS
    (SELECT CONVERT(Varchar(255), c.FirstName + ' ' + c.LastName),
        e.Title,
        e.EmployeeID,
        1,
        CONVERT(Varchar(255), c.FirstName + ' ' + c.LastName)
     FROM HumanResources.Employee AS e
          JOIN Person.Contact AS c ON e.ContactID = c.ContactID 
     WHERE e.EmployeeID = @InEmpID
   UNION ALL
     SELECT CONVERT(Varchar(255), REPLICATE ('| ' , EmployeeLevel) +
        c.FirstName + ' ' + c.LastName),
        e.Title,
        e.EmployeeID,
        EmployeeLevel + 1,
        CONVERT (Varchar(255), RTRIM(Sort) + '| ' + FirstName + ' ' + 
                 LastName)
     FROM HumanResources.Employee as e
          JOIN Person.Contact AS c ON e.ContactID = c.ContactID
          JOIN DirectReports AS d ON e.ManagerID = d.EmployeeID
    )
-- copy the required columns to the result of the function 
   INSERT @retFindReports
   SELECT EmployeeID, Name, Title, EmployeeLevel, Sort
   FROM DirectReports 
   RETURN
END;
GO
-- Example invocation
SELECT EmployeeID, Name, Title, EmployeeLevel
FROM dbo.fn_FindReports(109)
ORDER BY Sort;