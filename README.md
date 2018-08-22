# Selective C Preprocessor

C言語のプリプロセスを実行して指定したマクロ定義でifdefを削除します。

## 使い方

```SCPP -DDISABLE_MACRO -EENABLE_MACRO main.c```

```
void main()
{
#ifdef DISABLE_MACRO
    printf("delete");
#else
    printf("Hello ");
#endif
#ifdef ENABLE_MACRO
    printf("world!");
#else
    printf("delete");
#endif
}
```
が
```
void main()
{
    printf("Hello ");
    printf("world!");
}
```
となります。

## ライセンス

jay(Yacc)やselective C preprocessorをC#に移植して使用していますので、ファイルにあるライセンスに従ってください。
その他の部分は[MITライセンス](LICENSE)です。
