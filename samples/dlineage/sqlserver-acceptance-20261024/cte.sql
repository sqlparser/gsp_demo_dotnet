with MyCTE(x)as(select x='hello')select x from MyCTE ;

with MyCTE(x)as(select x='hello')select x from MyCTE
;
with MyCTE(x)as(select top 10 x = id from sysobjects)select x, maxx = (select max(x) from MyCTE), pct =                    100.0 * x / (select sum(x) from MyCTE)from MyCTE
;
with MyCTE(x)as(select x = convert(varchar(8000),'hello')union all select x + 'a' from MyCTE where len(x) < 100)select x from MyCTE order by x
;
select x = convert(varchar(8000),'hello')
;
select x + 'a' from MyCTE where len(x) < 100
;

with MyCTE(x)as(select x = convert(varchar(1000),'hello')union all select convert(varchar(1000),x + 'a') from MyCTE   where len(x) < 100)select x from MyCTE order by x
;
with MyCTE(x)as(select x = convert(varchar(1000),'hello')union all select x = convert(varchar(1000),'goodbye')union all select convert(varchar(1000),x + 'a') from MyCTE   where len(x) < 10)select x from MyCTE order by len(x), x
;
with MyCTE(x)as(select x = convert(varchar(1000),'hello')union all select convert(varchar(1000),x + 'a') from MyCTE where len(x) < 10union all select convert(varchar(1000),x + 'b') from MyCTE where len(x) < 10)select x from MyCTE order by len(x), x
;

with MyCTE(x)as(select x = convert(varchar(1000),'hello')union all select convert(varchar(1000),x + 'a') from MyCTE   where len(x) < 10 and (len(x) = 5 or x like '%a')union all select convert(varchar(1000),x + 'b') from MyCTE   where len(x) < 10 and (len(x) = 5 or x like '%b'))select x from MyCTE order by len(x), x
;

with MyCTE(i, x)as(select i = 0, x = convert(varchar(1000),'hello')union all select i = 1, convert(varchar(1000),x + 'a') from MyCTE   where len(x) < 10 and i in (0,1)union all select i = 2, convert(varchar(1000),x + 'b') from MyCTE   where len(x) < 10 and i in (0,2))select x from MyCTE order by len(x), x
;

with MyCTE(r1, r2, i, x)as(select r1 = 1, r2 = 1, i = 0, x = convert(varchar(1000),'hello')union all select r1 = r1 + 1, r2 = r2, i = 1, convert(varchar(1000),x + 'a') from MyCTE   where len(x) < 10 and i in (0,1)union all select r1 = r1, r2 = r2 + 1, i = 2, convert(varchar(1000),x + 'b') from MyCTE   where len(x) < 10 and i in (0,2))select r1, r2, x from MyCTE order by len(x), x
;
with MyCTE(r1, r2, i, x)as(select r1 = 1, r2 = 1, i = 0, x = convert(varchar(1000),'hello')union all select r1 = r1 + 1, r2 = r2, i = 1, convert(varchar(1000),x + 'a') from MyCTE   where i in (0,1) and R1 < 3 union all select r1 = r1, r2 = r2 + 1, i = 2, convert(varchar(1000),x + 'b') from MyCTE   where i in (0,2) and R2 < 6)select r1, r2, x from MyCTE order by len(x), x
;
with MyCTE(i)as(select i = 1union all select i = i + 1 from MyCTE where i < 100)select ifrom MyCTE order by i
;

with MyCTE(i)as(select i = 1union all select i = i + 1 from MyCTE where i < 1000)select i from MyCTE order by i
;
with MyCTE(i)as(select i = 1union all select i = i + 1 from MyCTE where i < 1000)select i from MyCTE order by i option (maxrecursion 1000)
;

declare @Sales table (TrDate datetime, Amount money)
insert @Sales select '20060501', 200
insert @Sales select '20060501', 400
insert @Sales select '20060502', 1200
select [day] = TrDate, sum(amount) total_sales from @sales group by TrDate order by TrDate

declare @Sales table (TrDate datetime, Amount money)
insert @Sales select '20060501', 200
insert @Sales select '20060501', 400
insert @Sales select '20060502', 1200

;with MyCTE(d)as(select d = convert(datetime,'20060101')union all select d = d + 1 from MyCTE 
where d < '20061231')select [day] = d.d, sum(coalesce(s.amount,0))from MyCTE d left join @sales s on s.TrDate = d.d group by d.d order by d.d option (maxrecursion 1000)


declare @s varchar(1000)
select @s = 'a,b,cd,ef,zzz,hello'
;with csvtbl(i,j)as(select i=1, j=charindex(',',@s+',')union all select i=j+1, j=charindex(',',@s+',',j+1) from csvtbl  
 where charindex(',',@s+',',j+1) <> 0)select substring(@s,i,j-i)from csvtbl 

declare @s varchar(1000)
select @s = 'a,b,cd,ef,zzz,hello';with csvtbl(i,j, s)as(select i=1, s=charindex(',',@s+','),  
 substring(@s, 1, charindex(',',@s+',')-1)union all select i=j+1, j=charindex(',',@s+',',j+1),   
 substring(@s, j+1, charindex(',',@s+',',j+1)-(j+1))from csvtbl where charindex(',',@s+',',j+1) <> 0)select s from csvtbl

GO

with n(rc,i)as(select rc = 1, i = 0union all select rc = 1, i = i + 1 
from n where rc = 1 and i < 32000union all select rc = 2, i = i + 32001 from n 
where rc = 1 and i < 32000)select count_i = count(*), max_i = max(i) from n option (maxrecursion 32000)
;

with n (j)as ( select j = 0 union all select j = j + 1 from n 
where j < 32000)select max_i = max (na.i), count_i = count(*)from 
( select i = j + k   from n   cross join   ( select k = j * 32001      from n      where j < 32   ) n2) na 
option (maxrecursion 32000)

