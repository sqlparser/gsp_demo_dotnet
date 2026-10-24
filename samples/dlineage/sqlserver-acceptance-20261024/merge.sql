MERGE Production.UnitMeasure AS target
    USING (SELECT @UnitMeasureCode, @Name) AS source (UnitMeasureCode, Name)
    ON (target.UnitMeasureCode = source.UnitMeasureCode)
    WHEN MATCHED THEN 
        UPDATE SET Name = source.Name
	WHEN NOT MATCHED THEN	
	    INSERT (UnitMeasureCode, Name)
	    VALUES (source.UnitMeasureCode, source.Name)
	    OUTPUT deleted.*, $action, inserted.* INTO #MyTempTable;


MERGE Production.ProductInventory AS target
USING (SELECT ProductID, SUM(OrderQty) FROM Sales.SalesOrderDetail AS sod
    JOIN Sales.SalesOrderHeader AS soh
    ON sod.SalesOrderID = soh.SalesOrderID
    AND soh.OrderDate = @OrderDate
    GROUP BY ProductID) AS source (ProductID, OrderQty)
ON (target.ProductID = source.ProductID)
WHEN MATCHED AND target.Quantity - source.OrderQty <= 0
    THEN DELETE
WHEN MATCHED 
    THEN UPDATE SET target.Quantity = target.Quantity - source.OrderQty, 
                    target.ModifiedDate = GETDATE()
OUTPUT $action, Inserted.ProductID, Inserted.Quantity, Inserted.ModifiedDate, Deleted.ProductID,
    Deleted.Quantity, Deleted.ModifiedDate;

MERGE INTO Sales.SalesReason AS Target
USING (VALUES ('Recommendation','Other'), ('Review', 'Marketing'), ('Internet', 'Promotion'))
       AS Source (NewName, NewReasonType)
ON Target.Name = Source.NewName
WHEN MATCHED THEN
	UPDATE SET ReasonType = Source.NewReasonType
WHEN NOT MATCHED BY TARGET THEN
	INSERT (Name, ReasonType) VALUES (NewName, NewReasonType)
OUTPUT $action INTO @SummaryOfChanges;

/*
INSERT INTO Production.UpdatedInventory
SELECT ProductID, LocationID, NewQty, PreviousQty 
FROM
(    MERGE Production.ProductInventory AS pi
     USING (SELECT ProductID, SUM(OrderQty) 
            FROM Sales.SalesOrderDetail AS sod
            JOIN Sales.SalesOrderHeader AS soh
            ON sod.SalesOrderID = soh.SalesOrderID
            AND soh.OrderDate BETWEEN '20030701' AND '20030731'
            GROUP BY ProductID) AS src (ProductID, OrderQty)
     ON pi.ProductID = src.ProductID
    WHEN MATCHED AND pi.Quantity - src.OrderQty >= 0 
        THEN UPDATE SET pi.Quantity = pi.Quantity - src.OrderQty
    WHEN MATCHED AND pi.Quantity - src.OrderQty <= 0 
        THEN DELETE
    OUTPUT $action, Inserted.ProductID, Inserted.LocationID, Inserted.Quantity AS NewQty, Deleted.Quantity AS PreviousQty)
 AS Changes (Action, ProductID, LocationID, NewQty, PreviousQty) WHERE Action = 'UPDATE';

*/

INSERT INTO dbo.MyProducts (Name, ListPrice)
VALUES ('Helmet', 25.50),
       ('Wheel', 30.00),
       (SELECT Name, ListPrice FROM Production.Product WHERE ProductID = 720);
       

MERGE INTO Portal.Account Dst

      USING (

            SELECT AccountID, AccountGuid, Name, [Synonym], TelephoneNumber, FaxNumber, CountryName,

                     VisitAddress, VisitAddressAdditional1, VisitAddressAdditional2, VisitAddressAdditional3,

                     VisitAddressPostalCode, VisitAddressCity, VisitAddressCountry,

                     PostalAddress, PostalAddressAdditional1, PostalAddressAdditional2, PostalAddressAdditional3,

                     PostalAddressPostalCode, PostalAddressCity, PostalAddressCountry,

                     EmailAddress, Website

            FROM   [Effectory CM].dbo.BaseAccounts

            WHERE  AccountID = 231) AS Src ON Src.AccountID = Dst.AccountID

      WHEN NOT MATCHED THEN INSERT VALUES

            (AccountID, AccountGuid, Name, [Synonym], TelephoneNumber, FaxNumber, CountryName,

             VisitAddress, VisitAddressAdditional1, VisitAddressAdditional2, VisitAddressAdditional3,

             VisitAddressPostalCode, VisitAddressCity, VisitAddressCountry,

             PostalAddress, PostalAddressAdditional1, PostalAddressAdditional2, PostalAddressAdditional3,

             PostalAddressPostalCode, PostalAddressCity, PostalAddressCountry,

             EmailAddress, Website)

      WHEN MATCHED THEN UPDATE SET

         AccountID = Src.AccountID, AccountGuid = Src.AccountGuid, Name = Src.Name, [Synonym] = Src.[Synonym],

         TelephoneNumber = Src.TelephoneNumber, FaxNumber = Src.FaxNumber, CountryName = Src.CountryName,

             VisitAddress = Src.VisitAddress, VisitAddressAdditional1 = Src.VisitAddressAdditional1, VisitAddressAdditional2 = Src.VisitAddressAdditional2,

             VisitAddressAdditional3 = Src.VisitAddressAdditional3, VisitAddressCity = Src.VisitAddressCity, VisitAddressCountry = Src.VisitAddressCountry,

             PostalAddress = Src.PostalAddress, PostalAddressAdditional1 = Src.PostalAddressAdditional1, PostalAddressAdditional2 = Src.PostalAddressAdditional2,

             PostalAddressAdditional3 = Src.PostalAddressAdditional3, PostalAddressCity = Src.PostalAddressCity, PostalAddressCountry = Src.PostalAddressCountry,

             EmailAddress = Src.EmailAddress, Website = Src.Website;

                    
MERGE INTO dbo.Customers AS TGT

USING dbo.CustomersStage AS SRC

  ON TGT.custid = SRC.custid

WHEN MATCHED THEN

  UPDATE SET

    TGT.companyname = SRC.companyname,

    TGT.phone = SRC.phone,

    TGT.address = SRC.address

WHEN NOT MATCHED THEN

  INSERT (custid, companyname, phone, address)

  VALUES (SRC.custid, SRC.companyname, SRC.phone, SRC.address)

WHEN NOT MATCHED BY SOURCE THEN

  DELETE

OUTPUT

  $action, deleted.custid AS del_custid, inserted.custid AS ins_custid;
  
                    