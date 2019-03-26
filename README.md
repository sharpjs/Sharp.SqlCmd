# Sharp.SqlCmd

A simple SQL preprocessor that supports a limited subset of
[SQLCMD](https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility) features:

* `GO` batch splitting

  ```sql
  SELECT 'This is a batch.';
  GO
  SELECT 'This is another batch.';
  ```

* `$( )` variable replacement

  ```sql
  SELECT * FROM $(TableName);
  ```

* `:r` inclusion

  ```sql
  :r C:\Foo.sql

  :r $(Path)\Foo.sql
  ```

* `:setvar` variable (re)definition

  ```sql
  :setvar TableName dbo.Customers

  -- With EnableVariableReplacementInSetvar = true (not supported by SQLCMD)
  :setvar TableName $(SchemaName).Customers
  ```

## Status

Experimental.
