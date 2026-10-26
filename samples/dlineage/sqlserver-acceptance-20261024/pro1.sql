CREATE PROCEDURE dbo.CreateOrdPart1 AS
begin
select Newid() as GUID, o.Prod_line, o.Prod_type,o.prod_fam,o.prod_code,o.prod_desc
into #OrdPart
from  ct_prod o 
insert OrdPart (OrdPart_Id,AddUser,AddDate,PartNum,description,Category,LineOfBusiness,PartType,PartNumType)
select dbo.StripGUID(GUID),'YCHENG',getdate(),o.Prod_code,o.Prod_desc,o.prod_line,'TFP',o.prod_fam,o.Prod_type
from #OrdPart o
end
GO


CREATE PROCEDURE dbo.CreateOrdPartLocation AS
begin
select Newid() as GUID,o.partnum,o.OrdPart_Id
into #OrdPartLocation
from  ordpart o 
where o.adduser= 'YCHENG' 
insert OrdPartLocation (OrdPartLoc_Id,OrdPart_Id,AddUser,AddDate,PartNum,PartFormat)
select dbo.StripGUID(GUID),OrdPart_Id,'YCHENG',getdate(),PartNum,'Physical'
from #OrdPartLocation
end
GO


CREATE PROCEDURE PrintRequest_GetAll 
AS
SET NOCOUNT ON
Select
PrintRequests.*,
ProjectType.ProjectTypeDesc,
MailClassType.MailClassTypeId
From
PrintRequests
Inner Join
ProjectType
On
PrintRequests.ProjectTypeId = ProjectType.ProjectTypeId
Inner Join
MailClassType
On
PrintRequests.MailClassTypeId = MailClassType.MailClassTypeId
Order By
CreateDate

GO

CREATE PROCEDURE UniqueSF  AS
Begin
SELECT     1 AS sortvalue, cast('All' AS varchar(20)) AS SalesForce
UNION
SELECT DISTINCT 2 AS sortvalue, Salesforce
FROM         salesarea
WHERE     salesforce IS NOT NULL
ORDER BY sortvalue, Salesforce
End


----------------------------------------------------------------------------------------
--              spGetShoppingList 
-----------------------------------------------------------------------------------------
-- Purpose: Returns a member's spGetShoppingList 
--          Can return a shopping list for a range of days e.g. days 2 to 4
-- 
-- Called From: 
-- 
-- I/P Parameters
--        Member ID
--        WeekNum   - which week
--        DayNum    - from day (1..7)
--        ToDayNum  - to day (1..7)

-- O/P Parameters
--        N/A
-- Calls to Other Programs:
--        N/A
-----------------------------------------------------------------------------------------
-- Revision History
-- Version           | Date   | Reason for Change
-----------------------------------------------------------------------------------------
-- Initial Version   | Jan 03 | Initial Version
-----------------------------------------------------------------------------------------

ALTER PROCEDURE spGetShoppingList 
(
  @p_member_id   INT,     
  @p_weeknum     INT,    
  @p_daynum      INT,
  @p_to_daynum   INT   
) 
AS

  DECLARE @v_startdate    DATETIME
  DECLARE @v_daynum       INT
  DECLARE @v_weekplan_id  INT
  DECLARE @v_weeknum      INT
  DECLARE @v_prevday      INT
  DECLARE @v_nextday      INT
  DECLARE @quest_name	    NVARCHAR(30)
  DECLARE @quantity	      INT

  SET NOCOUNT ON
  EXEC spTrackMember @p_member_id, '2.2', @p_weeknum


  -- If a member id has been passed THEN the current day AND week of 
  -- the program need to be determined
  
  IF ISNULL(@p_weeknum, 0) = 0 OR ISNULL(@p_daynum, 0) = 0
    BEGIN
      -- Get the Current Weekly Plan ID
      SELECT TOP 1 @v_weekplan_id = pk_wp_weeklyplan_id,
             @v_weeknum = wp_weeknumber,
             @v_startdate = wp_startdate
      FROM programme AS PG 
        INNER JOIN weeklyplan AS WP
        ON PG.pk_pg_program_id = WP.fk_wp_pg_program_id
      WHERE fk_pg_mb_member_id = @p_member_id
        AND pg_startdate < GETDATE()
        AND wp_startdate < GETDATE()
      ORDER BY pk_pg_program_id DESC, pk_wp_weeklyplan_id DESC
    
      -- Get current day ....
      SELECT @v_daynum = ABS(DATEDIFF(d, @v_startdate, getdate())) + 1
    END
  ELSE BEGIN
    -- initialise the daynumber AND the week variables...
    
    SET @v_daynum = @p_daynum
  
    SELECT TOP 1 @v_weekplan_id = pk_wp_weeklyplan_id
    FROM weeklyplan
    WHERE fk_wp_mb_member_id = @p_member_id
      AND wp_weeknumber = @p_weeknum
--      AND DATEDIFF(day, wp_startdate, GETDATE()) >= 0
    ORDER BY pk_wp_weeklyplan_id DESC
  
    SET @v_weeknum = @p_weeknum
  END

  -- Determine the next AND prev days....
  -- if at the start or end of the week set the prev/next day to zero....
  SET @v_prevday = @v_daynum - 1
  IF @v_daynum <= 7
    SET @v_nextday = @v_daynum + 1
  ELSE
    SET @v_nextday = 0

  DECLARE @v_tbl TABLE
  (
    foodname NVARCHAR(255),
    questid INT,
    questname INT,
    foodmeast INT,
    UOM NVARCHAR(20),
    foodcategory INT
  )

  INSERT INTO @v_tbl
	(
    foodname,
    questid,
    questname,
    foodmeast,
    UOM,
    foodcategory
  )  

  -- foodname

  SELECT 
    CASE 
    (
      SELECT count (mms.fk_mm_food_name_id) 
      FROM mm_shoppinglist_food AS MMS
      WHERE FD.pk_fd_food_id = MMS.fk_mm_food_id
    )
    WHEN 0 THEN 
	fd_food_name
--      (CASE ISNULL(FD.fd_shop_format,'') WHEN '' THEN FD.fd_food_name ELSE REPLACE(FD.fd_shop_format, '*', FD.fd_food_name) END)

--      dbo.fnFormatShopFood(FD.fd_food_name, FD.fd_shop_format, 1,1,1)
    ELSE  
      (
        SELECT fd_food_name 
        FROM food 
        INNER JOIN mm_shoppinglist_food AS MMS
          ON MMS.fk_mm_food_name_id = food.pk_fd_food_id
        WHERE MMS.fk_mm_food_id = FD.pk_fd_food_id
      )
		END,

  -- questid

  FD.fd_questid,
  
  -- questname

	CASE
		WHEN LK.pk_qg_questgroup_id = 1 THEN 1 --meat
		WHEN LK.pk_qg_questgroup_id = 2 THEN 2 --fish
		WHEN LK.pk_qg_questgroup_id = 3 THEN 3 --milk products
		WHEN LK.pk_qg_questgroup_id = 4 THEN 4 --fruit
		WHEN LK.pk_qg_questgroup_id = 5 THEN 5 --cereals
		WHEN LK.pk_qg_questgroup_id = 6 THEN 6 --breakfast cereals
		WHEN LK.pk_qg_questgroup_id = 7 THEN 7 --vegetables
		WHEN LK.pk_qg_questgroup_id = 8 THEN 8 --other
  END,

  -- foodmeast

  CASE ml.ml_recipe_flag	
        
  WHEN 'y' THEN	
        		-- CASE for scaleable
        		CASE 
              (
                SELECT rc_scaleable 
                FROM lk_recipe AS REC 
                  INNER JOIN ingredient AS ING
            		  ON REC.pk_rc_recipe_id = ING.fk_in_rc_recipe_id
          		  WHERE FD.pk_fd_food_id = ING.fk_in_fd_food_id 
                  AND ML.pk_ml_meal_id = REC.fk_rc_ml_meal_id
              )
          		WHEN 'Y' THEN 
          		(
                SELECT in_amount* MP.mp_quantity 
                FROM ingredient AS ING 
          			INNER JOIN lk_recipe AS REC  
                  ON REC.pk_rc_recipe_id = ING.fk_in_rc_recipe_id
          			WHERE FD.pk_fd_food_id = ING.fk_in_fd_food_id 
                  AND ML.pk_ml_meal_id = REC.fk_rc_ml_meal_id
              )
          		WHEN 'N' THEN
          		(
                SELECT in_amount 
                FROM ingredient AS ING 
          			  INNER JOIN lk_recipe AS REC  
                  ON REC.pk_rc_recipe_id = ING.fk_in_rc_recipe_id
          			WHERE FD.pk_fd_food_id = ING.fk_in_fd_food_id 
                  AND ML.pk_ml_meal_id = REC.fk_rc_ml_meal_id
              )
        		END
        		--end CASE for scaleable
        		WHEN 'n' THEN
        			(CASE mm.mm_fixedamount
            --nb *** changes made for toast
            WHEN 0  THEN --for fixed amount=0
            CASE 
            WHEN ml.fk_ml_mt_mealtype_id<>2 THEN --WHEN a snack
            
            CASE (SELECT mm_sl_use_new FROM mm_shoppinglist_food AS MMS INNER JOIN food
            ON MMS.fk_mm_food_name_id = food.pk_fd_food_id
            WHERE MMS.fk_mm_food_id = FD.pk_fd_food_id)
            WHEN 1 THEN -- for use new
            --(mm.mm_quantity * fd.fd_analysis_size *(SELECT mm_sl_ratio FROM mm_shoppinglist_food AS MMS INNER JOINfood
            (mm.mm_quantity * mp.mp_quantity *(SELECT mm_sl_ratio FROM mm_shoppinglist_food AS MMS INNER JOIN food
            ON MMS.fk_mm_food_name_id = food.pk_fd_food_id
            WHERE MMS.fk_mm_food_id = FD.pk_fd_food_id)
            )
            
          else    --( mm.mm_quantity * fd.fd_analysis_size)  -- for use new
                    ( mm.mm_quantity * mp.mp_quantity) 
          end
          else (mm.mm_quantity * ml.ml_serving_size) --WHEN a snack
          end
          
          WHEN 1 THEN  --for fixed amount
          CASE (SELECT mm_sl_use_new FROM mm_shoppinglist_food AS MMS INNER JOIN food
          ON MMS.fk_mm_food_name_id = food.pk_fd_food_id
          WHERE MMS.fk_mm_food_id = FD.pk_fd_food_id)
          WHEN 1 THEN 
        
          (mm.mm_quantity * ml.ml_serving_size
          * (SELECT mm_sl_ratio FROM mm_shoppinglist_food AS MMS INNER JOIN food
          ON MMS.fk_mm_food_name_id = food.pk_fd_food_id
          WHERE MMS.fk_mm_food_id = FD.pk_fd_food_id)
          )
          
        
        
          else    (mm.mm_quantity * ml.ml_serving_size)
          end
          --nb *** end changes made for toast
          
          			--WHEN 0 THEN   (mm.mm_quantity * fd.fd_analysis_size)
          			--WHEN 1 THEN   (mm.mm_quantity * ml.ml_serving_size)
          
          		end)end,

  -- UOM

  FD.fd_analysis_unitofmeast,

  -- foodcategory

  FD.fk_fd_fc_foodcategory_id
  
  FROM meal AS ML 
    INNER JOIN mealplan AS MP 
      ON ML.pk_ml_meal_id = MP.fk_mp_ml_meal_id
    INNER JOIN mm_meal_food AS MM
      ON MP.fk_mp_ml_meal_id = MM.fk_mm_ml_meal_id
    INNER JOIN food AS FD
      ON MM.fk_mm_fd_food_id = FD.pk_fd_food_id 
        AND FD.fk_fd_fc_foodcategory_id <> 4
    INNER JOIN lk_questgroup AS LK
      ON FD.fd_questid = LK.qg_questgroup_questid
  	
  WHERE MP.fk_mp_wp_weeklyplan_id = @v_weekplan_id
    AND (MP.mp_day_number BETWEEN @p_daynum AND @p_to_daynum)
--  GROUP BY FD.fd_food_name,
--    FD.fd_questid,
--   FD.pk_fd_food_id,
--    ML.pk_ml_meal_id,
--    ML.ml_recipe_flag,
--    MM.mm_fixedamount,
--    MM.mm_fixedamount,
--    ML.ml_recipe_flag,
--    MM.mm_quantity,
--    FD.fd_analysis_size,
--    MM.mm_quantity,
--    ML.ml_serving_size,
--    FD.fd_analysis_unitofmeast,
--    FD.fk_fd_fc_foodcategory_id,
--    LK.pk_qg_questgroup_id,
--    LK.qg_questgroup_name,
--    MP.mp_quantity,
--    ml.fk_ml_mt_mealtype_id



  -- Second insert into temp table WHERE foodcategory=4 ie composite

		INSERT INTO @v_tbl
		(foodname,questid,questname,foodmeast,UOM,foodcategory)
		SELECT 
      CASE (SELECT count (mms.fk_mm_food_name_id) FROM mm_shoppinglist_food AS MMS
      WHERE fd.pk_fd_food_id=MMS.fk_mm_food_id)
      		WHEN 0 THEN FD.fd_food_name
      		else  (
      SELECT fd_food_name FROM food INNER JOIN mm_shoppinglist_food AS MMS
      ON MMS.fk_mm_food_name_id = food.pk_fd_food_id
      WHERE MMS.fk_mm_food_id = fd.pk_fd_food_id
      )
		end,


    FD.fd_questid,
	  CASE 
		WHEN LK.pk_qg_questgroup_id=1 THEN 1--meat
		WHEN LK.pk_qg_questgroup_id=2 THEN 2--fish
		WHEN LK.pk_qg_questgroup_id=3 THEN 3--milk products
		WHEN LK.pk_qg_questgroup_id=4 THEN 4--fruit
		WHEN LK.pk_qg_questgroup_id=5 THEN 5--cereals
		WHEN LK.pk_qg_questgroup_id=6 THEN 6--breakfast cereals
		WHEN LK.pk_qg_questgroup_id=7 THEN 7--vegetables
		WHEN LK.pk_qg_questgroup_id=8 THEN 8--other

	  end,

			--this has been added to cater for ingredients of salad
                        -- if the meal is of type dinner THEN the salad ingredients are scaled
			CASE mp.fk_mp_mt_mealtype_id
					WHEN 7 THEN
                        CASE mm.mm_fixedamount
     			WHEN 0 THEN   (mmc.mm_cf_quantity  * mp.mp_quantity * mm.mm_quantity)
			WHEN 1 THEN   (mmc.mm_cf_quantity * ml.ml_serving_size * mm.mm_quantity)
			end
			else (mmc.mm_cf_quantity * ml.ml_serving_size)
			
                        end,
  FD.fd_analysis_unitofmeast,
FD.fk_fd_fc_foodcategory_id

  FROM   mm_meal_food AS mm INNER JOIN mealplan AS mp 
  	ON mm.fk_mm_ml_meal_id = mp.fk_mp_ml_meal_id 
  INNER JOIN food AS fd2
  	ON mm.fk_mm_fd_food_id = fd2.pk_fd_food_id 
  INNER JOIN mm_composite_food AS mmc 
  	ON fd2.pk_fd_food_id = mmc.fk_mm_fd_composite_food_id
  INNER JOIN food AS fd
  	ON fd.pk_fd_food_id =mmc.fk_mm_fd_food_id
  INNER JOIN meal AS ml 
  	ON mp.fk_mp_ml_meal_id = ml.pk_ml_meal_id
  INNER JOIN lk_questgroup AS LK
  	ON FD.fd_questid=LK.qg_questgroup_questid

  WHERE     MP.fk_mp_wp_weeklyplan_id = @v_weekplan_id
     AND   (MP.mp_day_number BETWEEN @p_daynum AND @p_to_daynum)AND(fd2.fk_fd_fc_foodcategory_id = 4)
--  GROUP BY mp.pk_mp_mealplan_id, 
--    fd.pk_fd_food_id, 
--    mmc.fk_mm_fd_food_id,
 --   fd.fd_food_name,
--    fd.fd_questid,
--    fd.fd_questid,
--    ml.pk_ml_meal_id,
--    ml.ml_recipe_flag,
--    mm.mm_quantity,
--    fd.fd_analysis_size,
--    mm.mm_fixedamount,
--    mm.mm_quantity,
--    ml.ml_serving_size,
--    mm.mm_fixedamount,
--    ml.ml_recipe_flag,
--    fd.fd_analysis_unitofmeast,
--    fd.fk_fd_fc_foodcategory_id,
--    LK.pk_qg_questgroup_id,
--    LK.qg_questgroup_name,
--    mmc.mm_cf_quantity,
--    fd2.fd_analysis_size,
--    mp.mp_quantity,
--    mp.fk_mp_mt_mealtype_id


  -- End temp table create
 

  -- SELECT MEAT
  SELECT foodname AS "Item",
    questid,
    questname,
    SUM(foodmeast) AS "Food Meast",
    UOM, 
    COUNT(foodname) AS "servings" 
  FROM @v_tbl
  WHERE questname = 1 
    AND foodcategory <> 2
  GROUP BY foodname, questid, questname, UOM
  
   --SELECT FISH
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE questname=2 AND foodcategory<>2
  GROUP BY foodname,questid,questname,UOM
  

   --SELECT MILK PRODUCTS
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE questname=3 AND foodcategory<>2
  GROUP BY foodname,questid,questname,UOM
  
   --SELECT VEGETABLES
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE questname=7 AND foodcategory<>2
  GROUP BY foodname,questid,questname,UOM
  
   --SELECT FRUIT
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE questname=4 AND foodcategory<>2
  GROUP BY foodname,questid,questname,UOM
  
   --SELECT CEREALS
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE questname=5 AND foodcategory<>2
  GROUP BY foodname,questid,questname,UOM
  
   --SELECT BREAKFAST CEREALS
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE questname=6 AND foodcategory<>2
  GROUP BY foodname,questid,questname,UOM
  
   --OTHER
  -- note: a new id=8 added to the lk_questgroup for other items,
  -- AND all base foods with questid = null, have been given a questid=62
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE questname=8 AND foodcategory<>2
  GROUP BY foodname,questid,questname,UOM
  
  --MILK ONLY
  --SELECT foodname AS "Item",questid,questname,
  --SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  --WHERE questid=9 AND foodcategory<>2
  --GROUP BY foodname,questid,questname,UOM
  
  --CONVENIENCES
  SELECT foodname AS "Item",questid,questname,
  SUM(foodmeast)as "Food Meast",UOM,count (foodname)as "servings" FROM @v_tbl
  WHERE foodcategory=2
  GROUP BY foodname,questid,questname,UOM
