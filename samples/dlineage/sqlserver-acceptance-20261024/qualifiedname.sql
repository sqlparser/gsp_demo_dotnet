-- 1
SELECT LastName
From .Employees;

-- 2
SELECT .Employees.LastName
From .Employees;

-- 3
SELECT .Employees.LastName
From .dbo.Employees;

-- 4
SELECT .dbo.Employees.LastName
From .dbo.Employees;

-- 5
SELECT .Employees.LastName.property1
From .dbo.Employees;

-- 6
SELECT .Employees.LastName.method1(1)
From .dbo.Employees;

-- 7
SELECT .db.dbo.Employees.LastName
From .dbo.Employees;

-- 8
SELECT .dbo.Employees.LastName.property1
From .dbo.Employees;

-- 9
SELECT .dbo.Employees.LastName.method1(1)
From .dbo.Employees;

-- 10
SELECT .db.dbo.Employees.LastName.property1
From .dbo.Employees;

-- 11
SELECT .db.dbo.Employees.LastName.method1(1)
From .dbo.Employees;

-- 12
SELECT .Employees.LastName
From .db.dbo.Employees;

-- 13
SELECT .dbo.Employees.LastName
From .db.dbo.Employees;

-- 14
SELECT .Employees.LastName.property1
From .db.dbo.Employees;

-- 15
SELECT .Employees.LastName.method1(1)
From .db.dbo.Employees;

-- 16
SELECT .db.dbo.Employees.LastName
From .db.dbo.Employees;

-- 17
SELECT .dbo.Employees.LastName.property1
From .db.dbo.Employees;

-- 18
SELECT .dbo.Employees.LastName.method1(1)
From .db.dbo.Employees;

-- 19
SELECT .db.dbo.Employees.LastName.property1
From .db.dbo.Employees;

-- 20
SELECT .db.dbo.Employees.LastName.method1(1)
From .db.dbo.Employees;

-- 21
SELECT E.LastName
From Employees E;

-- 22
SELECT Employees.LastName
From ..Employees;

-- 23
SELECT Employees.LastName
From ..dbo.Employees;

-- 24
SELECT ..Employees.LastName
From ..Employees;

-- 25
SELECT ..dbo.Employees.LastName
From ..Employees;

-- 26
SELECT ..Employees.LastName.property1
From ..Employees;

-- 27
SELECT ..Employees.LastName.method1(x)
From ..Employees;

-- 28
SELECT ..dbo.Employees.LastName.property1
From ..Employees;

-- 29
SELECT ..dbo.Employees.LastName.method1(x)
From ..Employees;

-- 30
SELECT Employees.LastName
From db..Employees;

-- 31
SELECT Employees.LastName
From myserver..dbo.Employees;

-- 32
SELECT db..Employees.LastName
From myserver..dbo.Employees;

-- 33
SELECT db..Employees.LastName.property1
From myserver..dbo.Employees;

-- 34
SELECT db..Employees.LastName.method1(x)
From myserver..dbo.Employees;

-- 35
SELECT myserver..dbo.Employees.LastName
From myserver..dbo.Employees;

-- 36
SELECT myserver..dbo.Employees.LastName.property1
From myserver..dbo.Employees;

-- 37
SELECT myserver..dbo.Employees.LastName.method1(x)
From myserver..dbo.Employees;

-- 38 
select * from b;
select b.* from b;
select scott.b.* from b;
select db1.scott.b.* from b;
select server1.db1.scott.b.* from b;