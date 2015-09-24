
CREATE TABLE [dbo].[LogNet](
	[DateUtc] [datetime] NULL,
	[Thread] [varchar](50) NULL,
	[Level] [varchar](50) NULL,
	[Logger] [varchar](200) NULL,
	[User] [varchar](50) NULL,
	[Message] [varchar](4000) NULL,
	[Exception] [varchar](4000) NULL
)
