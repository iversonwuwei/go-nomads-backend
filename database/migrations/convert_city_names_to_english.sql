-- ============================================================
-- 将城市名称从中文转换为英文
-- 中英文对照将通过前端国际化文件实现
-- ============================================================

-- 中国主要城市名称转换
UPDATE cities SET name = 'Beijing' WHERE name = '北京市' OR name = '北京';
UPDATE cities SET name = 'Shanghai' WHERE name = '上海市' OR name = '上海';
UPDATE cities SET name = 'Guangzhou' WHERE name = '广州市' OR name = '广州';
UPDATE cities SET name = 'Shenzhen' WHERE name = '深圳市' OR name = '深圳';
UPDATE cities SET name = 'Chengdu' WHERE name = '成都市' OR name = '成都';
UPDATE cities SET name = 'Hangzhou' WHERE name = '杭州市' OR name = '杭州';
UPDATE cities SET name = 'Chongqing' WHERE name = '重庆市' OR name = '重庆';
UPDATE cities SET name = 'Tianjin' WHERE name = '天津市' OR name = '天津';
UPDATE cities SET name = 'Nanjing' WHERE name = '南京市' OR name = '南京';
UPDATE cities SET name = 'Wuhan' WHERE name = '武汉市' OR name = '武汉';
UPDATE cities SET name = 'Xi''an' WHERE name = '西安市' OR name = '西安';
UPDATE cities SET name = 'Suzhou' WHERE name = '苏州市' OR name = '苏州';
UPDATE cities SET name = 'Dalian' WHERE name = '大连市' OR name = '大连';
UPDATE cities SET name = 'Qingdao' WHERE name = '青岛市' OR name = '青岛';
UPDATE cities SET name = 'Shenyang' WHERE name = '沈阳市' OR name = '沈阳';
UPDATE cities SET name = 'Xiamen' WHERE name = '厦门市' OR name = '厦门';
UPDATE cities SET name = 'Kunming' WHERE name = '昆明市' OR name = '昆明';
UPDATE cities SET name = 'Ningbo' WHERE name = '宁波市' OR name = '宁波';
UPDATE cities SET name = 'Changsha' WHERE name = '长沙市' OR name = '长沙';
UPDATE cities SET name = 'Zhengzhou' WHERE name = '郑州市' OR name = '郑州';
UPDATE cities SET name = 'Jinan' WHERE name = '济南市' OR name = '济南';
UPDATE cities SET name = 'Harbin' WHERE name = '哈尔滨市' OR name = '哈尔滨';
UPDATE cities SET name = 'Fuzhou' WHERE name = '福州市' OR name = '福州';
UPDATE cities SET name = 'Hefei' WHERE name = '合肥市' OR name = '合肥';
UPDATE cities SET name = 'Urumqi' WHERE name = '乌鲁木齐市' OR name = '乌鲁木齐';
UPDATE cities SET name = 'Lanzhou' WHERE name = '兰州市' OR name = '兰州';
UPDATE cities SET name = 'Nanchang' WHERE name = '南昌市' OR name = '南昌';
UPDATE cities SET name = 'Guiyang' WHERE name = '贵阳市' OR name = '贵阳';
UPDATE cities SET name = 'Taiyuan' WHERE name = '太原市' OR name = '太原';
UPDATE cities SET name = 'Shijiazhuang' WHERE name = '石家庄市' OR name = '石家庄';
UPDATE cities SET name = 'Nanning' WHERE name = '南宁市' OR name = '南宁';
UPDATE cities SET name = 'Hohhot' WHERE name = '呼和浩特市' OR name = '呼和浩特';
UPDATE cities SET name = 'Yinchuan' WHERE name = '银川市' OR name = '银川';
UPDATE cities SET name = 'Xining' WHERE name = '西宁市' OR name = '西宁';
UPDATE cities SET name = 'Lhasa' WHERE name = '拉萨市' OR name = '拉萨';
UPDATE cities SET name = 'Haikou' WHERE name = '海口市' OR name = '海口';
UPDATE cities SET name = 'Sanya' WHERE name = '三亚市' OR name = '三亚';

-- 中国其他常见城市
UPDATE cities SET name = 'Dongguan' WHERE name = '东莞市' OR name = '东莞';
UPDATE cities SET name = 'Foshan' WHERE name = '佛山市' OR name = '佛山';
UPDATE cities SET name = 'Wuxi' WHERE name = '无锡市' OR name = '无锡';
UPDATE cities SET name = 'Changchun' WHERE name = '长春市' OR name = '长春';
UPDATE cities SET name = 'Nantong' WHERE name = '南通市' OR name = '南通';
UPDATE cities SET name = 'Wenzhou' WHERE name = '温州市' OR name = '温州';
UPDATE cities SET name = 'Baoding' WHERE name = '保定市' OR name = '保定';
UPDATE cities SET name = 'Tangshan' WHERE name = '唐山市' OR name = '唐山';
UPDATE cities SET name = 'Yantai' WHERE name = '烟台市' OR name = '烟台';
UPDATE cities SET name = 'Zhuhai' WHERE name = '珠海市' OR name = '珠海';
UPDATE cities SET name = 'Huizhou' WHERE name = '惠州市' OR name = '惠州';
UPDATE cities SET name = 'Zhongshan' WHERE name = '中山市' OR name = '中山';
UPDATE cities SET name = 'Jiangmen' WHERE name = '江门市' OR name = '江门';
UPDATE cities SET name = 'Shaoxing' WHERE name = '绍兴市' OR name = '绍兴';
UPDATE cities SET name = 'Jiaxing' WHERE name = '嘉兴市' OR name = '嘉兴';
UPDATE cities SET name = 'Huzhou' WHERE name = '湖州市' OR name = '湖州';
UPDATE cities SET name = 'Taizhou' WHERE name = '台州市' OR name = '台州';
UPDATE cities SET name = 'Jinhua' WHERE name = '金华市' OR name = '金华';
UPDATE cities SET name = 'Yangzhou' WHERE name = '扬州市' OR name = '扬州';
UPDATE cities SET name = 'Zhenjiang' WHERE name = '镇江市' OR name = '镇江';
UPDATE cities SET name = 'Xuzhou' WHERE name = '徐州市' OR name = '徐州';
UPDATE cities SET name = 'Yancheng' WHERE name = '盐城市' OR name = '盐城';
UPDATE cities SET name = 'Lianyungang' WHERE name = '连云港市' OR name = '连云港';
UPDATE cities SET name = 'Huai''an' WHERE name = '淮安市' OR name = '淮安';
UPDATE cities SET name = 'Suqian' WHERE name = '宿迁市' OR name = '宿迁';
UPDATE cities SET name = 'Wuhu' WHERE name = '芜湖市' OR name = '芜湖';
UPDATE cities SET name = 'Bengbu' WHERE name = '蚌埠市' OR name = '蚌埠';
UPDATE cities SET name = 'Anqing' WHERE name = '安庆市' OR name = '安庆';
UPDATE cities SET name = 'Maanshan' WHERE name = '马鞍山市' OR name = '马鞍山';
UPDATE cities SET name = 'Quanzhou' WHERE name = '泉州市' OR name = '泉州';
UPDATE cities SET name = 'Zhangzhou' WHERE name = '漳州市' OR name = '漳州';
UPDATE cities SET name = 'Longyan' WHERE name = '龙岩市' OR name = '龙岩';
UPDATE cities SET name = 'Sanming' WHERE name = '三明市' OR name = '三明';
UPDATE cities SET name = 'Putian' WHERE name = '莆田市' OR name = '莆田';
UPDATE cities SET name = 'Nanping' WHERE name = '南平市' OR name = '南平';
UPDATE cities SET name = 'Ningde' WHERE name = '宁德市' OR name = '宁德';
UPDATE cities SET name = 'Ganzhou' WHERE name = '赣州市' OR name = '赣州';
UPDATE cities SET name = 'Jiujiang' WHERE name = '九江市' OR name = '九江';
UPDATE cities SET name = 'Shangrao' WHERE name = '上饶市' OR name = '上饶';
UPDATE cities SET name = 'Yichun' WHERE name = '宜春市' OR name = '宜春';
UPDATE cities SET name = 'Jingdezhen' WHERE name = '景德镇市' OR name = '景德镇';
UPDATE cities SET name = 'Xinyu' WHERE name = '新余市' OR name = '新余';
UPDATE cities SET name = 'Luoyang' WHERE name = '洛阳市' OR name = '洛阳';
UPDATE cities SET name = 'Kaifeng' WHERE name = '开封市' OR name = '开封';
UPDATE cities SET name = 'Anyang' WHERE name = '安阳市' OR name = '安阳';
UPDATE cities SET name = 'Xinxiang' WHERE name = '新乡市' OR name = '新乡';
UPDATE cities SET name = 'Zhoukou' WHERE name = '周口市' OR name = '周口';
UPDATE cities SET name = 'Nanyang' WHERE name = '南阳市' OR name = '南阳';
UPDATE cities SET name = 'Shangqiu' WHERE name = '商丘市' OR name = '商丘';
UPDATE cities SET name = 'Pingdingshan' WHERE name = '平顶山市' OR name = '平顶山';
UPDATE cities SET name = 'Xinyang' WHERE name = '信阳市' OR name = '信阳';
UPDATE cities SET name = 'Zhuzhou' WHERE name = '株洲市' OR name = '株洲';
UPDATE cities SET name = 'Xiangtan' WHERE name = '湘潭市' OR name = '湘潭';
UPDATE cities SET name = 'Hengyang' WHERE name = '衡阳市' OR name = '衡阳';
UPDATE cities SET name = 'Yueyang' WHERE name = '岳阳市' OR name = '岳阳';
UPDATE cities SET name = 'Changde' WHERE name = '常德市' OR name = '常德';
UPDATE cities SET name = 'Zhangjiajie' WHERE name = '张家界市' OR name = '张家界';
UPDATE cities SET name = 'Chenzhou' WHERE name = '郴州市' OR name = '郴州';
UPDATE cities SET name = 'Yiyang' WHERE name = '益阳市' OR name = '益阳';
UPDATE cities SET name = 'Loudi' WHERE name = '娄底市' OR name = '娄底';

-- 验证更新结果
SELECT 
    name,
    country,
    COUNT(*) as count
FROM cities 
WHERE country = 'China'
GROUP BY name, country
ORDER BY name
LIMIT 20;
