import 'package:flutter_test/flutter_test.dart';
import 'package:vlera_dent_app/main.dart';

void main() {
  testWidgets('Vlera Dent shell renders loading state', (tester) async {
    await tester.pumpWidget(const VleraDentApp());
    expect(find.text('Duke hapur Vlera Dent...'), findsOneWidget);
  });
}
