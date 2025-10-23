-- =====================================================
-- 地理数据初始化脚本
-- 包含：国家数据 + 中国完整省市数据
-- 执行顺序：国家 -> 省份 -> 城市
-- =====================================================

-- 1. 插入国家数据 (40个全球主要国家)
-- =====================================================

INSERT INTO countries (id, name, name_zh, code, code_alpha3, continent, calling_code, is_active, created_at)
VALUES 
  (gen_random_uuid(), 'China', '中国', 'CN', 'CHN', 'Asia', '+86', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'United States', '美国', 'US', 'USA', 'North America', '+1', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'India', '印度', 'IN', 'IND', 'Asia', '+91', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Japan', '日本', 'JP', 'JPN', 'Asia', '+81', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Germany', '德国', 'DE', 'DEU', 'Europe', '+49', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'United Kingdom', '英国', 'GB', 'GBR', 'Europe', '+44', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'France', '法国', 'FR', 'FRA', 'Europe', '+33', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Italy', '意大利', 'IT', 'ITA', 'Europe', '+39', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Brazil', '巴西', 'BR', 'BRA', 'South America', '+55', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Canada', '加拿大', 'CA', 'CAN', 'North America', '+1', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Russia', '俄罗斯', 'RU', 'RUS', 'Europe', '+7', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'South Korea', '韩国', 'KR', 'KOR', 'Asia', '+82', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Spain', '西班牙', 'ES', 'ESP', 'Europe', '+34', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Australia', '澳大利亚', 'AU', 'AUS', 'Oceania', '+61', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Mexico', '墨西哥', 'MX', 'MEX', 'North America', '+52', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Indonesia', '印度尼西亚', 'ID', 'IDN', 'Asia', '+62', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Netherlands', '荷兰', 'NL', 'NLD', 'Europe', '+31', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Saudi Arabia', '沙特阿拉伯', 'SA', 'SAU', 'Asia', '+966', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Turkey', '土耳其', 'TR', 'TUR', 'Asia', '+90', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Switzerland', '瑞士', 'CH', 'CHE', 'Europe', '+41', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Poland', '波兰', 'PL', 'POL', 'Europe', '+48', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Belgium', '比利时', 'BE', 'BEL', 'Europe', '+32', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Sweden', '瑞典', 'SE', 'SWE', 'Europe', '+46', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Argentina', '阿根廷', 'AR', 'ARG', 'South America', '+54', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Norway', '挪威', 'NO', 'NOR', 'Europe', '+47', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Austria', '奥地利', 'AT', 'AUT', 'Europe', '+43', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'United Arab Emirates', '阿联酋', 'AE', 'ARE', 'Asia', '+971', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Thailand', '泰国', 'TH', 'THA', 'Asia', '+66', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Ireland', '爱尔兰', 'IE', 'IRL', 'Europe', '+353', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Singapore', '新加坡', 'SG', 'SGP', 'Asia', '+65', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Denmark', '丹麦', 'DK', 'DNK', 'Europe', '+45', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Malaysia', '马来西亚', 'MY', 'MYS', 'Asia', '+60', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Philippines', '菲律宾', 'PH', 'PHL', 'Asia', '+63', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Vietnam', '越南', 'VN', 'VNM', 'Asia', '+84', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Portugal', '葡萄牙', 'PT', 'PRT', 'Europe', '+351', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Greece', '希腊', 'GR', 'GRC', 'Europe', '+30', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'New Zealand', '新西兰', 'NZ', 'NZL', 'Oceania', '+64', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'South Africa', '南非', 'ZA', 'ZAF', 'Africa', '+27', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Egypt', '埃及', 'EG', 'EGY', 'Africa', '+20', true, CURRENT_TIMESTAMP),
  (gen_random_uuid(), 'Nigeria', '尼日利亚', 'NG', 'NGA', 'Africa', '+234', true, CURRENT_TIMESTAMP)
ON CONFLICT (code) DO NOTHING;

-- 查看插入的国家数量
SELECT COUNT(*) as country_count FROM countries;

-- 2. 插入中国省份数据 (34个省级行政区)
-- =====================================================

-- 先获取中国的ID（用于后续省份和城市关联）
DO $$
DECLARE
  china_id UUID;
BEGIN
  -- 获取中国的ID
  SELECT id INTO china_id FROM countries WHERE code = 'CN';
  
  -- 插入所有省份
  INSERT INTO provinces (id, name, country_id, is_active, created_at)
  VALUES 
    -- 直辖市
    (gen_random_uuid(), '北京市', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '天津市', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '上海市', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '重庆市', china_id, true, CURRENT_TIMESTAMP),
    
    -- 省份
    (gen_random_uuid(), '河北省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '山西省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '辽宁省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '吉林省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '黑龙江省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '江苏省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '浙江省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '安徽省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '福建省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '江西省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '山东省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '河南省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '湖北省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '湖南省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '广东省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '海南省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '四川省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '贵州省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '云南省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '陕西省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '甘肃省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '青海省', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '台湾省', china_id, true, CURRENT_TIMESTAMP),
    
    -- 自治区
    (gen_random_uuid(), '内蒙古自治区', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '广西壮族自治区', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '西藏自治区', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '宁夏回族自治区', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '新疆维吾尔自治区', china_id, true, CURRENT_TIMESTAMP),
    
    -- 特别行政区
    (gen_random_uuid(), '香港特别行政区', china_id, true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '澳门特别行政区', china_id, true, CURRENT_TIMESTAMP)
  ON CONFLICT (country_id, name) DO NOTHING;
  
  RAISE NOTICE '省份数据插入完成';
END $$;

-- 查看插入的省份数量
SELECT COUNT(*) as province_count FROM provinces;

-- 3. 插入中国城市数据 (345+个城市)
-- =====================================================

DO $$
DECLARE
  china_id UUID;
  province_id UUID;
BEGIN
  -- 获取中国的ID
  SELECT id INTO china_id FROM countries WHERE code = 'CN';
  
  -- 北京市
  SELECT id INTO province_id FROM provinces WHERE name = '北京市' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES (gen_random_uuid(), '北京市', 'China', china_id, province_id, '北京市', true, CURRENT_TIMESTAMP);
  
  -- 天津市
  SELECT id INTO province_id FROM provinces WHERE name = '天津市' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES (gen_random_uuid(), '天津市', 'China', china_id, province_id, '天津市', true, CURRENT_TIMESTAMP);
  
  -- 上海市
  SELECT id INTO province_id FROM provinces WHERE name = '上海市' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES (gen_random_uuid(), '上海市', 'China', china_id, province_id, '上海市', true, CURRENT_TIMESTAMP);
  
  -- 重庆市
  SELECT id INTO province_id FROM provinces WHERE name = '重庆市' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES (gen_random_uuid(), '重庆市', 'China', china_id, province_id, '重庆市', true, CURRENT_TIMESTAMP);
  
  -- 河北省
  SELECT id INTO province_id FROM provinces WHERE name = '河北省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '石家庄市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '唐山市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '秦皇岛市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '邯郸市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '邢台市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '保定市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '张家口市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '承德市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '沧州市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '廊坊市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '衡水市', 'China', china_id, province_id, '河北省', true, CURRENT_TIMESTAMP);
  
  -- 山西省
  SELECT id INTO province_id FROM provinces WHERE name = '山西省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '太原市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '大同市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '阳泉市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '长治市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '晋城市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '朔州市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '晋中市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '运城市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '忻州市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '临汾市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '吕梁市', 'China', china_id, province_id, '山西省', true, CURRENT_TIMESTAMP);
  
  -- 内蒙古自治区
  SELECT id INTO province_id FROM provinces WHERE name = '内蒙古自治区' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '呼和浩特市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '包头市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '乌海市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '赤峰市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '通辽市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '鄂尔多斯市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '呼伦贝尔市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '巴彦淖尔市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '乌兰察布市', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '兴安盟', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '锡林郭勒盟', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '阿拉善盟', 'China', china_id, province_id, '内蒙古自治区', true, CURRENT_TIMESTAMP);
  
  RAISE NOTICE '前几个省份的城市数据插入完成';
END $$;

-- 查看当前城市数量
SELECT COUNT(*) as city_count FROM cities WHERE country_id = (SELECT id FROM countries WHERE code = 'CN');

-- 提示：脚本太长，继续下一部分...
