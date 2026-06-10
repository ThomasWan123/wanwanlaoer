# Release 与 Debug 共用 WebView 桥；R8 不得裁掉 Native 接口
-keep class com.sanguotd.GameWebView { *; }
-keepclassmembers class com.sanguotd.GameWebView$NativeBridge {
    @android.webkit.JavascriptInterface <methods>;
}
