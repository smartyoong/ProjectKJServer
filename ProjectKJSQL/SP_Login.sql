USE [AccountDB]
GO

/****** Object:  StoredProcedure [dbo].[SP_Login]    Script Date: 2023-11-15 오후 9:15:02 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[SP_Login]
    @ID VARCHAR(50),
    @PW VARCHAR(50),
	@NickName NVARCHAR(16) OUTPUT
AS
BEGIN
    DECLARE @result INT = 0;
	DECLARE @Count INT = 0;

    IF EXISTS (SELECT * FROM TBL_ACCOUNT WHERE ID = @ID)
        SET @result = 1;
	IF(SELECT PWDCOMPARE(@PW,PW) FROM TBL_ACCOUNT WHERE ID = @ID) = 1
	BEGIN
		SET @result = 2;
	END
	SET @NickName = (SELECT TBL_ACCOUNT.NICKNAME FROM TBL_ACCOUNT WHERE ID = @ID);
	/*result 0 : no account , 1 : Password Wrong, 2 : Success*/
    RETURN @result;
END
GO

