-- Oracle proprietary outer-join syntax. Feed this to convertJoin to see it
-- rewritten as ANSI JOIN.
--   dotnet run --project src/demos/convertJoin/demos.convertJoin.csproj -c Release -- /f samples/oracle-outer-join.sql /t oracle

SELECT e.employee_id,
       e.last_name,
       d.department_name,
       l.city
FROM   employees e,
       departments d,
       locations l
WHERE  e.department_id = d.department_id(+)
AND    d.location_id   = l.location_id(+)
AND    e.salary > 5000
ORDER BY e.last_name;
