-- A report query parameterised with $name$ placeholders, the form the
-- removeCondition demo rewrites. removeCondition's Main is hard-wired to
-- C:\1.txt, so it is a code sample rather than something to run against this
-- file -- see src/demos/removeCondition/README.md.
--
-- removevars takes a positional path (no /f, no /t -- the vendor is hard-coded
-- to MSSQL) and prints the rewrite to stdout:
--   dotnet run --project src/demos/removevars/demos.removevars.csproj -c Release -- samples/oracle-bind-variables.sql
-- It leaves the predicates below in place: $name$ inside quotes is a string
-- literal, not a bind marker, and removevars only strips real bind variables.

SELECT SUM(d.amt) AS total_amount,
       d.fund_acct,
       d.cntrb_date
FROM   summit.cntrb_detail d
WHERE  d.fund_coll_attrb IN ( '$Institute$' )
AND    d.fund_acct       IN ( '$Fund$' )
AND    d.cntrb_date     >= '$From_Date$'
AND    d.cntrb_date     <= '$Thru_Date$'
GROUP  BY d.fund_acct, d.cntrb_date;
