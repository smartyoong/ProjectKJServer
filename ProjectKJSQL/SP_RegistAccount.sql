USE [AccountDB]
GO

/****** Object:  StoredProcedure [dbo].[SP_RegistAccount]    Script Date: 2023-11-15 오후 9:15:19 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[SP_RegistAccount]
    @ID VARCHAR(50),
    @PW VARCHAR(50),
	@IP VARCHAR(50)
AS
BEGIN
	/*ID 중복 검사*/
    DECLARE @ErrorCode INT;
    SET @ErrorCode = (SELECT COUNT(*) FROM TBL_ACCOUNT WHERE ID = @ID);
	IF @ErrorCode > 0
	BEGIN
		RETURN -1;
	END

	/*비밀 번호 규칙 검사는 클라이언트에서 진행한다. 테이블에 삽입*/

	BEGIN TRY
        INSERT INTO TBL_ACCOUNT (ID, PW, IP) VALUES (@ID, PWDENCRYPT(@PW), @IP);
    END TRY
    BEGIN CATCH
        PRINT '에러가 발생하였습니다: ' + ERROR_MESSAGE();
		RETURN -2
    END CATCH


    RETURN 0;
END
GO

