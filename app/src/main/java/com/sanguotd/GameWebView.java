package com.sanguotd;

import android.app.Activity;
import android.content.Context;
import android.util.AttributeSet;
import android.os.Build;
import android.os.VibrationEffect;
import android.os.Vibrator;
import android.os.VibratorManager;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

public class GameWebView extends WebView {

    private static final String ASSET_HOME_PREFIX = "file:///android_asset/";

    private Activity activity;

    public GameWebView(Context context) {
        super(context);
    }

    public GameWebView(Context context, AttributeSet attrs) {
        super(context, attrs);
    }

    public GameWebView(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
    }

    public void init(Activity activity) {
        this.activity = activity;

        WebSettings settings = getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setAllowFileAccess(true);
        settings.setAllowFileAccessFromFileURLs(true);
        settings.setAllowUniversalAccessFromFileURLs(true);
        settings.setAllowContentAccess(false);
        settings.setMediaPlaybackRequiresUserGesture(false);
        settings.setCacheMode(WebSettings.LOAD_NO_CACHE);
        settings.setRenderPriority(WebSettings.RenderPriority.HIGH);
        settings.setSupportZoom(false);
        settings.setBuiltInZoomControls(false);
        settings.setDisplayZoomControls(false);
        settings.setUseWideViewPort(true);
        settings.setLoadWithOverviewMode(true);

        setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, String url) {
                return !isAllowedAssetUrl(url);
            }

            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                if (request != null && request.getUrl() != null) {
                    return !isAllowedAssetUrl(request.getUrl().toString());
                }
                return super.shouldOverrideUrlLoading(view, request);
            }
        });
        setWebChromeClient(new WebChromeClient());

        addJavascriptInterface(new NativeBridge(), "Native");

        setVerticalScrollBarEnabled(false);
        setHorizontalScrollBarEnabled(false);
        setOverScrollMode(OVER_SCROLL_NEVER);

        WebView.setWebContentsDebuggingEnabled(BuildConfig.DEBUG);

        loadUrl(ASSET_HOME_PREFIX + "www/index.html");
    }

    private static boolean isAllowedAssetUrl(String url) {
        return url != null && url.startsWith(ASSET_HOME_PREFIX);
    }

    private class NativeBridge {

        @JavascriptInterface
        public void vibrate(int milliseconds) {
            Vibrator vib;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                VibratorManager vm = (VibratorManager)
                    activity.getSystemService(Context.VIBRATOR_MANAGER_SERVICE);
                vib = vm.getDefaultVibrator();
            } else {
                vib = (Vibrator) activity.getSystemService(Context.VIBRATOR_SERVICE);
            }
            if (vib == null || !vib.hasVibrator()) return;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                vib.vibrate(VibrationEffect.createOneShot(
                    milliseconds, VibrationEffect.DEFAULT_AMPLITUDE));
            } else {
                vib.vibrate(milliseconds);
            }
        }

        @JavascriptInterface
        public void exitApp() {
            activity.finish();
        }

        @JavascriptInterface
        public void keepScreenOn(boolean on) {
            activity.runOnUiThread(() -> {
                if (on) {
                    activity.getWindow().addFlags(
                        android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
                } else {
                    activity.getWindow().clearFlags(
                        android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
                }
            });
        }
    }
}
