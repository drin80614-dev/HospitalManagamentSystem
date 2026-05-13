import 'dart:async';
import 'dart:io' show Platform;

import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:webview_flutter/webview_flutter.dart';
import 'package:webview_windows/webview_windows.dart' as windows;

const String defaultBackendUrl =
    'https://hospitalmanagamentsystem.onrender.com/Auth/Login';

const String backendUrl = String.fromEnvironment(
  'BACKEND_URL',
  defaultValue: defaultBackendUrl,
);

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  SystemChrome.setPreferredOrientations(const [DeviceOrientation.portraitUp]);
  runApp(const VleraDentApp());
}

class VleraDentApp extends StatelessWidget {
  const VleraDentApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Vlera Dent',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF1395D3),
          primary: const Color(0xFF159BD7),
          secondary: const Color(0xFF2E3192),
          surface: const Color(0xFFF6FBFF),
        ),
        scaffoldBackgroundColor: const Color(0xFFEFFAFF),
        fontFamily: 'Arial',
      ),
      home: const ClinicShell(),
    );
  }
}

class ClinicShell extends StatefulWidget {
  const ClinicShell({super.key});

  @override
  State<ClinicShell> createState() => _ClinicShellState();
}

class _ClinicShellState extends State<ClinicShell> {
  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage();
  final Uri _backendUri = Uri.parse(backendUrl);
  StreamSubscription<List<ConnectivityResult>>? _connectivitySubscription;
  bool _offline = false;

  @override
  void initState() {
    super.initState();
    _rememberBackend();
    _watchConnectivity();
  }

  Future<void> _rememberBackend() async {
    await _secureStorage.write(key: 'vlera_dent_backend_url', value: backendUrl);
  }

  void _watchConnectivity() {
    _connectivitySubscription =
        Connectivity().onConnectivityChanged.listen((results) {
      final isOffline =
          results.isEmpty || results.every((item) => item == ConnectivityResult.none);
      if (mounted && isOffline != _offline) {
        setState(() => _offline = isOffline);
      }
    });
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_offline) {
      return AppFrame(
        child: OfflineState(onRetry: () => setState(() => _offline = false)),
      );
    }

    if (kIsWeb) {
      return const AppFrame(
        child: UnsupportedState(
          message: 'Ky aplikacion eshte per Android, iPhone dhe Windows.',
        ),
      );
    }

    if (Platform.isWindows) {
      return AppFrame(
        child: WindowsClinicWebView(initialUrl: _backendUri.toString()),
      );
    }

    if (Platform.isAndroid || Platform.isIOS) {
      return MobileClinicWebView(initialUrl: _backendUri);
    }

    return const AppFrame(
      child: UnsupportedState(
        message: 'Kjo platforme nuk eshte e konfiguruar per Vlera Dent.',
      ),
    );
  }
}

class MobileClinicWebView extends StatefulWidget {
  const MobileClinicWebView({super.key, required this.initialUrl});

  final Uri initialUrl;

  @override
  State<MobileClinicWebView> createState() => _MobileClinicWebViewState();
}

class _MobileClinicWebViewState extends State<MobileClinicWebView> {
  late final WebViewController _controller;
  bool _loading = true;
  bool _hasError = false;

  @override
  void initState() {
    super.initState();
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setNavigationDelegate(
        NavigationDelegate(
          onPageStarted: (_) => setState(() {
            _loading = true;
            _hasError = false;
          }),
          onPageFinished: (_) => setState(() => _loading = false),
          onWebResourceError: (_) => setState(() {
            _loading = false;
            _hasError = true;
          }),
          onNavigationRequest: (request) {
            final uri = Uri.tryParse(request.url);
            if (uri == null) {
              return NavigationDecision.prevent;
            }

            if (uri.host == widget.initialUrl.host ||
                request.url.startsWith('about:blank')) {
              return NavigationDecision.navigate;
            }

            launchUrl(uri, mode: LaunchMode.externalApplication);
            return NavigationDecision.prevent;
          },
        ),
      )
      ..loadRequest(widget.initialUrl);
  }

  @override
  Widget build(BuildContext context) {
    return AppFrame(
      child: Stack(
        children: [
          WebViewWidget(controller: _controller),
          if (_loading) const LoadingState(),
          if (_hasError)
            OfflineState(
              onRetry: () {
                setState(() {
                  _hasError = false;
                  _loading = true;
                });
                _controller.reload();
              },
            ),
        ],
      ),
    );
  }
}

class WindowsClinicWebView extends StatefulWidget {
  const WindowsClinicWebView({super.key, required this.initialUrl});

  final String initialUrl;

  @override
  State<WindowsClinicWebView> createState() => _WindowsClinicWebViewState();
}

class _WindowsClinicWebViewState extends State<WindowsClinicWebView> {
  final windows.WebviewController _controller = windows.WebviewController();
  bool _ready = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _initialize();
  }

  Future<void> _initialize() async {
    try {
      await _controller.initialize();
      await _controller.setBackgroundColor(Colors.transparent);
      await _controller.setPopupWindowPolicy(
        windows.WebviewPopupWindowPolicy.deny,
      );
      await _controller.loadUrl(widget.initialUrl);
      if (mounted) {
        setState(() => _ready = true);
      }
    } catch (error) {
      if (mounted) {
        setState(() => _errorMessage = error.toString());
      }
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_errorMessage != null) {
      return OfflineState(
        details: _errorMessage,
        onRetry: () {
          setState(() {
            _errorMessage = null;
            _ready = false;
          });
          _initialize();
        },
      );
    }

    return Stack(
      children: [
        if (_ready) windows.Webview(_controller) else const SizedBox.expand(),
        if (!_ready) const LoadingState(),
      ],
    );
  }
}

class AppFrame extends StatelessWidget {
  const AppFrame({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final isDesktop = width >= 900;

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            if (isDesktop) const DesktopTitleBar(),
            Expanded(child: child),
          ],
        ),
      ),
    );
  }
}

class DesktopTitleBar extends StatelessWidget {
  const DesktopTitleBar({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 54,
      padding: const EdgeInsets.symmetric(horizontal: 18),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: Color(0xFFD7EAF6))),
      ),
      child: Row(
        children: [
          Image.asset('assets/brand/vlera-dent-app-icon-192.png', height: 34),
          const SizedBox(width: 12),
          const Text(
            'Vlera Dent',
            style: TextStyle(
              fontWeight: FontWeight.w800,
              fontSize: 18,
              color: Color(0xFF16233A),
            ),
          ),
          const Spacer(),
          const Icon(Icons.cloud_done_outlined, color: Color(0xFF159BD7)),
          const SizedBox(width: 8),
          const Text(
            'Render backend + Cloudflare D1',
            style: TextStyle(color: Color(0xFF607089), fontSize: 13),
          ),
        ],
      ),
    );
  }
}

class LoadingState extends StatelessWidget {
  const LoadingState({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: const Color(0xEEF6FBFF),
      child: Center(
        child: Container(
          width: 340,
          padding: const EdgeInsets.all(28),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(28),
            boxShadow: const [
              BoxShadow(
                color: Color(0x220A7DB8),
                blurRadius: 28,
                offset: Offset(0, 18),
              ),
            ],
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Image.asset('assets/brand/vlera-dent-app-icon-512.png', height: 74),
              const SizedBox(height: 22),
              const LinearProgressIndicator(minHeight: 6),
              const SizedBox(height: 18),
              const Text(
                'Duke hapur Vlera Dent...',
                style: TextStyle(
                  fontWeight: FontWeight.w800,
                  fontSize: 18,
                  color: Color(0xFF16233A),
                ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Po lidhemi me backend-in ne Render dhe D1.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Color(0xFF607089)),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class OfflineState extends StatelessWidget {
  const OfflineState({super.key, required this.onRetry, this.details});

  final VoidCallback onRetry;
  final String? details;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      height: double.infinity,
      padding: const EdgeInsets.all(24),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          colors: [Color(0xFFEFFAFF), Color(0xFFFFFFFF)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 460),
          child: Container(
            padding: const EdgeInsets.all(28),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(28),
              border: Border.all(color: const Color(0xFFD7EAF6)),
              boxShadow: const [
                BoxShadow(
                  color: Color(0x180A7DB8),
                  blurRadius: 30,
                  offset: Offset(0, 18),
                ),
              ],
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Image.asset('assets/brand/vlera-dent-app-icon-512.png', height: 82),
                const SizedBox(height: 20),
                const Text(
                  'Nuk u be lidhja',
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.w900,
                    color: Color(0xFF16233A),
                  ),
                ),
                const SizedBox(height: 10),
                const Text(
                  'Kontrollo internetin ose prit pak derisa Render ta zgjoje backend-in.',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 16, color: Color(0xFF607089)),
                ),
                if (details != null) ...[
                  const SizedBox(height: 14),
                  Text(
                    details!,
                    textAlign: TextAlign.center,
                    maxLines: 4,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 12, color: Color(0xFF8B98AA)),
                  ),
                ],
                const SizedBox(height: 22),
                FilledButton.icon(
                  onPressed: onRetry,
                  icon: const Icon(Icons.refresh),
                  label: const Text('Provo perseri'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class UnsupportedState extends StatelessWidget {
  const UnsupportedState({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Text(
          message,
          textAlign: TextAlign.center,
          style: const TextStyle(fontSize: 18, color: Color(0xFF16233A)),
        ),
      ),
    );
  }
}
