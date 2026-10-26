-- #Input 195.134.158.74 07/15/05 01:27:23 AM Mssql sql parse:ok pp:ok  pp error detected

DECLARE @mut_tbl datetime , @tran_flag int, @error int SELECT @tran_flag = @@trancount IF @tran_flag = 0 BEGIN TRANSACTION lock_tran ELSE SAVE TRANSACTION lock_tran SELECT @mut_tbl = mut_dat FROM turnus HOLDLOCK WHERE turnus_id = 4212 IF convert(char,@mut_tbl, 109) <> '' BEGIN IF @mut_tbl = Convert(datetime, 'JUL 15 2005 01:08:49:317', 109) BEGIN UPDATE turnus SET intervall_groesse = 2 , intervall_art = 1 , intervall_ausfuehrung_art = NULL , intervall_referenz_jahr = NULL , mut_usr = USER , mut_dat = GETDATE() , intervall_zusatz = NULL , intervall_tag = NULL , intervall_monat = NULL , intervall_wochentag = 1 , intervall_referenz_dat = '15.07.2005' , intervall_referenz_monat = NULL , intervall_erstmals = '15.07.2005' , intervall_letztmals = NULL WHERE turnus_id = 4212 SELECT @error = @@ERROR IF @error = 0 BEGIN SELECT 0 END END ELSE BEGIN SELECT 1 END END ELSE BEGIN SELECT 2 END IF @tran_flag = 0 AND @error = 0 COMMIT TRANSACTION lock_tran ELSE IF @tran_flag = 0 ROLLBACK TRANSACTION lock_tran 

-- #Input 195.134.158.74 07/15/05 01:27:51 AM Mssql sql parse:ok pp:ok  pp error detected

DECLARE @mut_tbl datetime , @tran_flag int, @error int SELECT @tran_flag = @@trancount IF @tran_flag = 0 BEGIN TRANSACTION lock_tran ELSE SAVE TRANSACTION lock_tran SELECT @mut_tbl = mut_dat FROM turnus HOLDLOCK WHERE turnus_id = 4212 IF convert(char,@mut_tbl, 109) <> '' BEGIN IF @mut_tbl = Convert(datetime, 'JUL 15 2005 01:08:49:317', 109) BEGIN UPDATE turnus SET intervall_groesse = 2 , intervall_art = 1 , intervall_ausfuehrung_art = NULL , intervall_referenz_jahr = NULL , mut_usr = USER , mut_dat = GETDATE() , intervall_zusatz = NULL , intervall_tag = NULL , intervall_monat = NULL , intervall_wochentag = 1 , intervall_referenz_dat = '15.07.2005' , intervall_referenz_monat = NULL , intervall_erstmals = '15.07.2005' , intervall_letztmals = NULL WHERE turnus_id = 4212 SELECT @error = @@ERROR IF @error = 0 BEGIN SELECT 0 END END ELSE BEGIN SELECT 1 END END ELSE BEGIN SELECT 2 END IF @tran_flag = 0 AND @error = 0 COMMIT TRANSACTION lock_tran ELSE IF @tran_flag = 0 ROLLBACK TRANSACTION lock_tran 

-- #Input 218.219.247.177 07/19/05 05:33:00 AM Mssql sql parse:ok pp:ok  pp error detected

 select '2004/07/31 0:00:00', 
T9.Corp_ID, T9.Customer_Code, T4.Customer_Name, T9.Accept_No, 
T9.Accept_Date, T9.Goods_Code, T9.Item_NO, T9.Tag_NO, T9.Special_Value, 
T9.Sequencial_NO, T9.Color_ID, T9.YYMM, T9.Accept_Balance_Nos, 
T9.Fixed_Sale_Nos, T9.Accept_Sale_Price, T4.staff_code, T9.Purchase_code, T9.Tag_Price, T4.Transfer_Customer_Code 
FROM     MST_Customer T4 
LEFT JOIN ( select T1.Corp_ID, T1.Customer_Code, T1.Accept_No, T1.Accept_Date, T2.Goods_Code, T2.Item_NO, T2.Tag_NO, T2.Special_Value, T2.Sequencial_NO, T2.Color_ID, T2.YYMM, T2.Accept_Nos - T2.Return_Nos - T2.Fixed_Sale_Nos Accept_Balance_Nos, T2.Fixed_Sale_Nos, T2.Accept_Sale_Price, T3.Purchase_code, T3.Ta
g_Price from Acc_Data T1, Acc_Detail_Data T2, MST_Goods T3 where T2.Sequencial_NO = T3.Sequencial_NO AND T2.Special_Value = T3.Special_Value AND T2.Tag_NO = T3.Tag_NO AND T2.Item_NO = T3.Item_NO and T1.Accept_No = T2.Accept_No AND T1.Corp_ID = T2.Corp_ID ) T9 
ON T4.Customer_Code = T9.Customer_Code;



-- #Input 195.68.122.182 07/20/05 03:08:54 AM Mssql sql parse:ok pp:ok  pp error detected

SELECT CONTACT.CONT_PKEY, CONTACT.CONT_Prenom, CONT_Nom FROM CONTACT JOIN Gestion_Contact ON Gestion_Contact.CONT_PKEY = CONTACT.CONT_PKEY 
WHERE Gestion_Contact.DESK_PKEY='1' AND Gestion_Contact.SECT_PKEY='1';



-- #Input 63.119.207.251 07/12/05 05:35:29 AM Mssql sql parse:ok pp:ok  pp error detected

CREATE PROCEDURE SP_RDELETE 

(@SDELETE AS VARCHAR(2000),   --DELETE SQL STATEMENT AS PARAMETER 

@SCOMMENT AS VARCHAR(1000),   --COMMENT/TITLE FOR THE DELETING RECORD(S) 

@ERRORMESSAGE AS VARCHAR(2000) --ERROR MESSAGE IF ANY DURING THE SP EXECUTION 

) 

AS 

  

--VARIABLE DECLARATION 

DECLARE @SSTR AS VARCHAR(2000),     --VARIABLE TO CONSTRUCT SQL STATEMENTS 

@SSELECT AS VARCHAR(2000),    --VARIABLE TO CONSTRUCT 'SELECT' SQL 

@STABLE AS VARCHAR(2000),     --VARIABLE TO STORE TABLE NAME 

@SPOS AS INT                  --VARIABLE TO STORE CHARACTER POSITIONS 

  

--TRIMMING OF DATA 

SET @SDELETE = ' '+ LTRIM(RTRIM(@SDELETE)) + ' ' 

  

--COPY OF @SDELETE PARAMETER TO EXECUTE 'SELECT' SQL STATEMENT 

SET @SSELECT = @SDELETE 

  

--CONSTRUCT 'SELECT' SQL FROM @SDELETE PARAMETER 

--USING 'SELECT' SQL COPY THE DELETING RECORDS TO RECYCLE TABLE COPY 

SET @SPOS = CHARINDEX(' DELETE ', @SSELECT) 

  

IF @SPOS > 0 

BEGIN 

      IF CHARINDEX(' FROM ', @SSELECT, @SPOS) = 0 

      BEGIN 

            SET @STABLE = LTRIM(RTRIM(SUBSTRING(@SSELECT, @SPOS + 7, 2000))) 

            SET @SSELECT = ' SELECT * FROM ' + @STABLE 

      END 

      ELSE 

      BEGIN 

            SET @SSTR = RTRIM(LTRIM(SUBSTRING(@SSELECT, CHARINDEX(' FROM ', @SSELECT) + 6, 2000))) 

            IF CHARINDEX(' ', @SSTR) = 0 

            BEGIN 

                  SET @SSELECT = ' SELECT * FROM ' + @SSTR 

                  SET @STABLE = @SSTR 

            END 

            ELSE 

            BEGIN 

                  SET @STABLE = SUBSTRING(@SSTR, 1, CHARINDEX(' ', @SSTR)) 

                  SET @SSELECT = REPLACE(@SSELECT, ' DELETE ', ' SELECT * ') 

            END 

      END 

END 

ELSE 

BEGIN 

      SET @ERRORMESSAGE = 'NO DELETE KEYWORD IN THE SDELETE PARAMETER' 

      RETURN -1 

END 

  

--USING 'SELECT' SQL COPY THE DELETING RECORDS TO RECYCLE TABLE COPY 

SET @STABLE = 'RECYCLE_' + @STABLE 

--CHECK WHETHER RECYCLE TABLE FOR DELETING RECORDS TABLE ALREADY EXIST 

--IF DOES NOT EXIST CREATE ONE 

IF (SELECT 1 FROM INFORMATION_SCHEMA.TABLES 

WHERE TABLE_NAME = @STABLE ) > 0 

BEGIN       

      --TABLE ALREADY EXIST 

      --BUILDING THE SQL: INSERT INTO EXISTING RECYLE TABLE 

--USING 'INSERT..SELECT' SQL 

      SET @SSELECT = REPLACE (@SSELECT, ' SELECT * ', ' INSERT INTO ' + @STABLE + ' SELECT *, (SELECT MAX(TRANSNO)+1 FROM ' + @STABLE + '), ''' +  @SCOMMENT + ''' ') 

END 

ELSE  --TABLE DOES NOT EXIST 

BEGIN 

      --BUILDING THE SQL: CREATE TABLE USING 'SELECT INTO' SQL 

      SET @SSELECT = REPLACE (@SSELECT, ' SELECT * ', ' SELECT *, 1 TRANSNO, ''' + @SCOMMENT + ''' COMMENT INTO ' + @STABLE + ' ' ) 

END 

  

BEGIN TRANSACTION TRANS_SP_DELETE 

  

--EXECUTE THE CONSTRUCTED SELECT SQL THAT CREATES/INSERTS RECYCLE TABLE 

EXEC(@SSELECT) 

--CHECK FOR ERRORS 

IF @@ERROR > 0 

BEGIN 

      SET @ERRORMESSAGE = 'ERROR WHILE CREATING/UPDATING RECYCLE TABLE' 

      ROLLBACK TRANSACTION TRANS_SP_DELETE 

      RETURN -1 

END 

  

--EXECUTE THE ACTUAL DELETE STATEMENT NOW 

EXEC (@SDELETE) 

--CHECK FOR ERRORS 

IF @@ERROR > 0 

BEGIN 

      SET @ERRORMESSAGE = 'ERROR WHILE EXECUTING PASSED DELETE SQL STATEMENT' 

      ROLLBACK TRANSACTION TRANS_SP_DELETE 

      RETURN -1 

END 

ELSE 

BEGIN 

      --COMPLETED SUCCESSFULLY 

      COMMIT TRANSACTION TRANS_SP_DELETE 

      RETURN 0 

END 


-- #Input 82.154.114.237 08/01/05 04:01:24 PM Mssql vb/asp parse:ok pp:ok  pp error detected

SELECT     dbo.NUMERO_SERIE.NS_PR_COD, dbo.NUMERO_SERIE.NS_COD, 
                      dbo.MARCA.MA_NOME + '--' + dbo.TIPO_PRODUTO.TP_NOME + '--' + dbo.PRODUTO.PR_DESC + '--' + dbo.NUMERO_SERIE.NS_SERIE AS equipamento
FROM         (SELECT     teste.NS_COD, teste.EX_LO_COD
                       FROM          dbo.EXISTENCIA INNER JOIN
                                              dbo.NUMERO_SERIE ON dbo.EXISTENCIA.EX_NS_COD = dbo.NUMERO_SERIE.NS_COD INNER JOIN
                                                  (SELECT     dbo.NUMERO_SERIE.NS_COD, dbo.EXISTENCIA.EX_LO_COD
                                                    FROM          dbo.NUMERO_SERIE INNER JOIN
                                                                           dbo.PRODUTO ON dbo.NUMERO_SERIE.NS_PR_COD = dbo.PRODUTO.PR_COD INNER JOIN
                                                                           dbo.EXISTENCIA ON dbo.NUMERO_SERIE.NS_COD = dbo.EXISTENCIA.EX_NS_COD AND 
                                                                           dbo.PRODUTO.PR_COD = dbo.EXISTENCIA.EX_PR_COD LEFT OUTER JOIN
                                                                           dbo.ASSOCIA ON dbo.NUMERO_SERIE.NS_COD = dbo.ASSOCIA.ASS_NSFILHO_COD
                                                    WHERE      (dbo.PRODUTO.PR_GERAL = 0) AND (dbo.ASSOCIA.ASS_ESTADO IS NULL OR
                                                                           dbo.ASSOCIA.ASS_ESTADO = 0) AND (dbo.EXISTENCIA.EX_QUANT > 0)) teste ON 
                                              dbo.EXISTENCIA.EX_LO_COD = teste.EX_LO_COD COLLATE Latin1_General_CI_AS
                       WHERE      (dbo.EXISTENCIA.EX_NS_COD = 156) AND (teste.NS_COD <> 156)
                       UNION ALL
                       SELECT     dbo.NUMERO_SERIE.NS_COD, dbo.EXISTENCIA.EX_LO_COD
                       FROM         dbo.PRODUTO INNER JOIN
                                             dbo.NUMERO_SERIE ON dbo.PRODUTO.PR_COD = dbo.NUMERO_SERIE.NS_PR_COD INNER JOIN
                                             dbo.EXISTENCIA ON dbo.PRODUTO.PR_COD = dbo.EXISTENCIA.EX_PR_COD AND 
                                             dbo.NUMERO_SERIE.NS_COD = dbo.EXISTENCIA.EX_NS_COD
                       WHERE     (dbo.PRODUTO.PR_GERAL = 1)) DERIVEDTBL INNER JOIN
                      dbo.NUMERO_SERIE ON DERIVEDTBL.NS_COD = dbo.NUMERO_SERIE.NS_COD INNER JOIN
                      dbo.PRODUTO ON dbo.NUMERO_SERIE.NS_PR_COD = dbo.PRODUTO.PR_COD INNER JOIN
                      dbo.TIPO_PRODUTO ON dbo.PRODUTO.PR_TP_COD = dbo.TIPO_PRODUTO.TP_COD INNER JOIN
                      dbo.MARCA ON dbo.PRODUTO.PR_MA_COD = dbo.MARCA.MA_COD
WHERE     (dbo.MARCA.MA_NOME LIKE N'%%') OR
                      (dbo.TIPO_PRODUTO.TP_NOME LIKE N'%%') OR
                      (dbo.PRODUTO.PR_DESC LIKE N'%%') OR
                      (dbo.NUMERO_SERIE.NS_SERIE LIKE N'%%')
