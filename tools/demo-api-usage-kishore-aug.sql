-- DEMO API usage for Kishore Finance (code KIS), August 2026. Safe to re-run: it clears its own DEMO rows first.
-- Every row has ProviderRef 'DEMO-...' so it can be removed cleanly (see the bottom).
USE los_lms;

DELETE FROM ApiUsageLog WHERE ProviderRef LIKE 'DEMO-%';

INSERT INTO ApiUsageLog (CompanyId, ApiCode, ApplicationId, ProviderRef, IsSuccess, UnitRate, CreatedAt)
WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM n WHERE i < 30),
mix(code, cnt, fail) AS (
    SELECT 'KYC_DIGILOCKER',      18, 2 UNION ALL
    SELECT 'KYC_ID_OCR',          22, 1 UNION ALL
    SELECT 'KYC_ID_VERIFY',       20, 0 UNION ALL
    SELECT 'KYC_AADHAAR_MASK',    12, 0 UNION ALL
    SELECT 'KYC_SELFIE',           9, 1 UNION ALL
    SELECT 'KYC_BUSINESS',         6, 1 UNION ALL
    SELECT 'BANK_PENNY_DROP',     14, 3 UNION ALL
    SELECT 'BANK_STMT_ANALYZER',   5, 0 UNION ALL
    SELECT 'ESIGN_AADHAAR',        8, 0 UNION ALL
    SELECT 'MANDATE_API',          7, 1
)
SELECT (SELECT Id FROM Companies WHERE Code = 'KIS'),
       m.code,
       NULL,
       CONCAT('DEMO-', m.code, '-', n.i),
       n.i <= m.cnt - m.fail,
       r.UnitRate,
       -- 5-10 UTC = 10:30-15:30 IST, so every row stays inside August in India time.
       TIMESTAMP('2026-08-01 05:00:00') + INTERVAL MOD(n.i * 5 + LENGTH(m.code), 30) DAY + INTERVAL MOD(n.i, 6) HOUR
FROM mix m
JOIN n ON n.i <= m.cnt
JOIN ApiRate r ON r.Code = m.code;

SELECT r.Product, r.Name, SUM(l.IsSuccess) AS billed_calls, SUM(1 - l.IsSuccess) AS failed,
       SUM(l.IsSuccess * l.UnitRate) AS amount
FROM ApiUsageLog l JOIN ApiRate r ON r.Code = l.ApiCode
WHERE l.ProviderRef LIKE 'DEMO-%'
GROUP BY r.Product, r.Name ORDER BY r.Product, r.Name;

-- ---- TO REMOVE THE DEMO DATA AGAIN (uncomment and run) ----
-- DELETE FROM ApiUsageLog WHERE ProviderRef LIKE 'DEMO-%';
-- DELETE FROM ApiInvoice WHERE PeriodStart = '2026-08-01' AND CompanyId = (SELECT Id FROM Companies WHERE Code = 'KIS');
