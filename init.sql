CREATE DATABASE techblog2 DEFAULT CHARSET utf8mb4;
USE techblog2;

CREATE TABLE techblog2.base_resource
	(
	  Id          BIGINT UNSIGNED NOT NULL
	, IsDeleted   TINYINT NOT NULL DEFAULT 0
	, Name        VARCHAR (150) NOT NULL
	, Size        BIGINT UNSIGNED NOT NULL
	, Path        VARCHAR (500) NOT NULL
	, ContentType VARCHAR (100) NOT NULL
	, MD5         VARCHAR (50) NOT NULL
	, Uploader_Id BIGINT UNSIGNED NOT NULL
	, UploadTime  DATETIME NOT NULL
	, PRIMARY KEY (Id)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.base_role
	(
	  Id        BIGINT UNSIGNED NOT NULL
	, IsDeleted TINYINT NOT NULL DEFAULT 0
	, Encode    VARCHAR (20) NOT NULL
	, Name      VARCHAR (20) NOT NULL
	, IsDefault TINYINT NOT NULL DEFAULT 0
	, PRIMARY KEY (Id)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.base_userrole
	(
	  User_Id BIGINT UNSIGNED NOT NULL
	, Role_Id BIGINT UNSIGNED NOT NULL
	, PRIMARY KEY (User_Id, Role_Id)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.blog_articles
	(
	  Id          BIGINT UNSIGNED NOT NULL
	, IsDeleted   TINYINT NOT NULL DEFAULT 0
	, Title       VARCHAR (100) NOT NULL
	, Content     MEDIUMTEXT NOT NULL
	, Category_Id BIGINT UNSIGNED NOT NULL
	, ViewsCount  INT NOT NULL
	, CreateTime  DATETIME NOT NULL
	, ModifyTime  DATETIME
	, PRIMARY KEY (Id)
	, KEY IX_Blog_Articles_Category_Id (Category_Id)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.blog_articletags
	(
	  Article_Id BIGINT UNSIGNED NOT NULL
	, Tag_Id     BIGINT UNSIGNED NOT NULL
	, PRIMARY KEY (Article_Id, Tag_Id)
	, KEY IX_Blog_ArticleTags_Tag_Id (Tag_Id)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.blog_categories
	(
	  Id         BIGINT UNSIGNED NOT NULL
	, IsDeleted  TINYINT NOT NULL DEFAULT 0
	, Name       VARCHAR (20) NOT NULL
	, CreateTime DATETIME NOT NULL
	, PRIMARY KEY (Id)
	, KEY IX_Blog_Categories_Name (Name)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.blog_comments
	(
	  Id          BIGINT UNSIGNED NOT NULL
	, IsDeleted   TINYINT NOT NULL DEFAULT 0
	, User_Id     BIGINT NOT NULL
	, ReplyTo     VARCHAR(20) NULL
	, Article_Id  BIGINT UNSIGNED NOT NULL
	, Parent_Id   BIGINT UNSIGNED
	, Content     VARCHAR (1000) NOT NULL
	, CommentTime DATETIME NOT NULL
	, ModifyTime  DATETIME
	, PRIMARY KEY (Id)
	, KEY IX_Blog_Comments_Article_Id (Article_Id)
	, KEY IX_Blog_Comments_User_Id (User_Id)
	, KEY IX_Blog_Comments_Parent_Id (Parent_Id)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.blog_tags
	(
	  Id        BIGINT UNSIGNED NOT NULL
	, IsDeleted TINYINT NOT NULL DEFAULT 0
	, TKey      VARCHAR (20) NOT NULL
	, Name      VARCHAR (20) NOT NULL
	, PRIMARY KEY (Id)
	, KEY IX_Blog_Tags_TKey (TKey)
	) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE techblog2.base_user
    (
      Id         BIGINT UNSIGNED NOT NULL
	, IsDeleted  TINYINT(1) NOT NULL DEFAULT 0
    , Account    VARCHAR(15) NOT NULL
    , Name       VARCHAR(20) NOT NULL
    , Hash       VARCHAR(64) NOT NULL
    , Salt       VARCHAR(64) NOT NULL
    , Email      VARCHAR(50) NOT NULL
    , Avatar_Id  BIGINT UNSIGNED NOT NULL
	, IsDisabled TINYINT(1) NOT NULL DEFAULT 0
	, IsAdmin    TINYINT(1) NOT NULL DEFAULT 0
    ) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;