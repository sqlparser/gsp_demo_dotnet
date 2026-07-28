-- A report query parameterised with $name$ placeholders, the form the
-- removevars and removeCondition demos rewrite.
--   dotnet run --project src/demos/removevars/demos.removevars.csproj -c Release -- /f samples/oracle-bind-variables.sql /t oracle

SELECT SUM(d.amt) AS total_amount,
       d.fund_acct,
       d.cntrb_date
FROM   summit.cntrb_detail d
WHERE  d.fund_coll_attrb IN ( '$Institute$' )
AND    d.fund_acct       IN ( '$Fund$' )
AND    d.cntrb_date     >= '$From_Date$'
AND    d.cntrb_date     <= '$Thru_Date$'
GROUP  BY d.fund_acct, d.cntrb_date;
