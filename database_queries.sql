SELECT *
  FROM [ECommerceDb].[dbo].[Products]

  SELECT *
  FROM [ECommerceDb].[dbo].[Carts]
   where UserId = 1

  SELECT *
  FROM [ECommerceDb].[dbo].[CartItems]

    SELECT *
  FROM [ECommerceDb].[dbo].Categories

  -- ---- 

    SELECT *
  FROM [ECommerceDb].[dbo].Orders
 
     SELECT *
  FROM [ECommerceDb].[dbo].OrderItems

   
     SELECT *
  FROM [ECommerceDb].[dbo].Users

-- --------------------------------------------------------------------------
-- End of database queries
-- --------------------------------------------------------------------------   


-- ---------------------------------------------------------------------------
-- Start of database inserts/Updates
-- ---------------------------------------------------------------------------

USE [ECommerceDb]
GO

INSERT INTO [dbo].[CartItems]
           ([Quantity]
           ,[CartId]
           ,[ProductId])
     VALUES
           (1
           ,1
           ,2)
GO
-- -------------------------------------------
USE [ECommerceDb]
GO

UPDATE [dbo].[Products]
   SET [Name] = 'laptop'
      ,[Description] = 'smart iPhone'
      ,[Price] = 200.00
      ,[StockQuantity] = 1000
      ,[ImageUrl] = 'https://istore.com/laptop/MACBook'
      ,[CategoryId] = 2
 WHERE Id = 3
GO
-- ---------------------------------------------------

USE [ECommerceDb]
GO

INSERT INTO [dbo].[Products]
           ([Name]
           ,[Description]
           ,[Price]
           ,[StockQuantity]
           ,[ImageUrl]
           ,[CategoryId])
     VALUES
           ('MAC Book'
           ,'laptop'
           ,200
           ,100
           ,'https://istore.com/laptop/MACBook'
           ,2)
GO




-- ---------------------------------------------------------------------------
-- End of database inserts
-- ---------------------------------------------------------------------------