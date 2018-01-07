

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Sys_Demo](
	[DemoId] [int] NOT NULL,
	[DemoCode] [nvarchar](32) NULL,
	[DemoName] [nvarchar](32) NOT NULL,
	[DemoChar] [char](2) NOT NULL,
	[DemoChar_Nullable] [char](2) NULL,
	[DemoByte] [tinyint] NOT NULL,
	[DemoByte_Nullable] [tinyint] NULL,
	[DemoDateTime] [datetime] NOT NULL,
	[DemoDateTime_Nullable] [datetime] NULL,
	[DemoDecimal] [decimal](18, 2) NOT NULL,
	[DemoDecimal_Nullable] [decimal](18, 2) NULL,
	[DemoFloat] [float] NOT NULL,
	[DemoFloat_Nullable] [float] NULL,
	[DemoReal] [real] NOT NULL,
	[Demo_Nullable] [real] NULL,
	[DemoGuid] [uniqueidentifier] NOT NULL,
	[DemoGuid_Nullable] [uniqueidentifier] NULL,
	[DemoShort] [smallint] NOT NULL,
	[DemoShort_Nullable] [smallint] NULL,
	[DemoInt] [int] NOT NULL,
	[DemoInt_Nullable] [int] NULL,
	[DemoLong] [bigint] NOT NULL,
	[DemoLong_Nullable] [bigint] NULL,
 CONSTRAINT [PK_Sys_Demo] PRIMARY KEY CLUSTERED 
(
	[DemoId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

Delete Sys_Demo 
Go
Insert Into [Sys_Demo] ([DemoId],[DemoCode],[DemoName],[DemoChar],[DemoChar_Nullable],[DemoByte],[DemoByte_Nullable],[DemoDateTime],[DemoDateTime_Nullable],[DemoDecimal],[DemoDecimal_Nullable],[DemoFloat],[DemoFloat_Nullable],[DemoReal],[Demo_Nullable],[DemoGuid],[DemoGuid_Nullable],[DemoShort],[DemoShort_Nullable],[DemoInt],[DemoInt_Nullable],[DemoLong],[DemoLong_Nullable]) Values (1,N'001',N'001','KB',NULL,8,NULL,'01  1 2017 12:00AM',NULL,16.00,NULL,32,NULL,64,NULL,'9e110f40-a784-4dd1-bf30-e67f76ec6a78',NULL,128,NULL,256,NULL,512,NULL)
Insert Into [Sys_Demo] ([DemoId],[DemoCode],[DemoName],[DemoChar],[DemoChar_Nullable],[DemoByte],[DemoByte_Nullable],[DemoDateTime],[DemoDateTime_Nullable],[DemoDecimal],[DemoDecimal_Nullable],[DemoFloat],[DemoFloat_Nullable],[DemoReal],[Demo_Nullable],[DemoGuid],[DemoGuid_Nullable],[DemoShort],[DemoShort_Nullable],[DemoInt],[DemoInt_Nullable],[DemoLong],[DemoLong_Nullable]) Values (2,N'002',N'002','KB','KB',8,8,'01  1 2017 12:00AM','01  1 2017 12:00AM',16.00,16.00,32,32,64,64,'9e110f40-a784-4dd1-bf30-e67f76ec6a78','9e110f40-a784-4dd1-bf30-e67f76ec6a78',128,128,256,256,512,512)
Go



SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Sys_Thin](
	[ThinId] [int] NOT NULL,
	[ThinName] [nvarchar](64) NOT NULL,
 CONSTRAINT [PK_Sys_Thin] PRIMARY KEY CLUSTERED 
(
	[ThinId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Sys_ThinIdentity](
	[ThinId] [int] IDENTITY(1,1) NOT NULL,
	[ThinName] [nvarchar](64) NOT NULL
) ON [PRIMARY]

GO
