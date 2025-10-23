-- =====================================================
-- 地理数据初始化脚本 - 第2部分
-- 继续插入中国其他省份的城市数据
-- =====================================================

DO $$
DECLARE
  china_id UUID;
  province_id UUID;
BEGIN
  -- 获取中国的ID
  SELECT id INTO china_id FROM countries WHERE code = 'CN';
  
  -- 辽宁省
  SELECT id INTO province_id FROM provinces WHERE name = '辽宁省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '沈阳市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '大连市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '鞍山市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '抚顺市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '本溪市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '丹东市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '锦州市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '营口市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '阜新市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '辽阳市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '盘锦市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '铁岭市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '朝阳市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '葫芦岛市', 'China', china_id, province_id, '辽宁省', true, CURRENT_TIMESTAMP);
  
  -- 吉林省
  SELECT id INTO province_id FROM provinces WHERE name = '吉林省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '长春市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '吉林市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '四平市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '辽源市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '通化市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '白山市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '松原市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '白城市', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '延边朝鲜族自治州', 'China', china_id, province_id, '吉林省', true, CURRENT_TIMESTAMP);
  
  -- 黑龙江省
  SELECT id INTO province_id FROM provinces WHERE name = '黑龙江省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '哈尔滨市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '齐齐哈尔市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '鸡西市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '鹤岗市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '双鸭山市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '大庆市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '伊春市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '佳木斯市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '七台河市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '牡丹江市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '黑河市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '绥化市', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '大兴安岭地区', 'China', china_id, province_id, '黑龙江省', true, CURRENT_TIMESTAMP);
  
  -- 江苏省
  SELECT id INTO province_id FROM provinces WHERE name = '江苏省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '南京市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '无锡市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '徐州市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '常州市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '苏州市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '南通市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '连云港市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '淮安市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '盐城市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '扬州市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '镇江市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '泰州市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '宿迁市', 'China', china_id, province_id, '江苏省', true, CURRENT_TIMESTAMP);
  
  -- 浙江省
  SELECT id INTO province_id FROM provinces WHERE name = '浙江省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '杭州市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '宁波市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '温州市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '嘉兴市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '湖州市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '绍兴市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '金华市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '衢州市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '舟山市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '台州市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '丽水市', 'China', china_id, province_id, '浙江省', true, CURRENT_TIMESTAMP);
  
  -- 安徽省
  SELECT id INTO province_id FROM provinces WHERE name = '安徽省' AND country_id = china_id;
  INSERT INTO cities (id, name, country, country_id, province_id, region, is_active, created_at)
  VALUES 
    (gen_random_uuid(), '合肥市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '芜湖市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '蚌埠市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '淮南市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '马鞍山市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '淮北市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '铜陵市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '安庆市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '黄山市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '滁州市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '阜阳市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '宿州市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '六安市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '亳州市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '池州市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), '宣城市', 'China', china_id, province_id, '安徽省', true, CURRENT_TIMESTAMP);
  
  RAISE NOTICE '东部省份城市数据插入完成';
END $$;

-- 查看当前城市总数
SELECT COUNT(*) as city_count FROM cities WHERE country_id = (SELECT id FROM countries WHERE code = 'CN');
