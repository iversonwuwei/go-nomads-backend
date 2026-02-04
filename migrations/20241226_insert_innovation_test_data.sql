-- 插入创新项目测试数据
-- Migration: 20241226_insert_innovation_test_data.sql
-- 用户 ID: bffcd353-d6ea-48ea-899d-967bd958cdbe (walden.wuwei@163.com)

-- 项目1: 智课通 - AI学习伙伴
INSERT INTO public.innovations (
    id, title, description, elevator_pitch, problem, solution,
    target_audience, product_type, key_features, competitive_advantage,
    business_model, market_opportunity, ask,
    creator_id, category, stage, team_size, is_public, is_featured,
    like_count, view_count, comment_count
) VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567001',
    '智课通',
    '面向大学生的AI学习伙伴，提供个性化辅导服务',
    '我们是面向大学生的AI学习伙伴，像私人tutor一样个性化辅导，但完全自动化且价格更低。',
    '大学生备考四六级时缺乏个性化练习和及时反馈，导致复习效率低下、通过率不高。',
    '我们开发了一款基于AI的备考App，能根据用户错题自动推荐学习路径，并生成每日训练计划，提升学习效率30%以上。',
    '主要用户：一二线城市的大二至大四本科生
次要用户：考研学生、语言培训机构
用户画像：年龄18-24岁，手机使用频繁，愿意为提分付费',
    '微信小程序 + 后台管理系统',
    '智能错题分析与知识点定位
个性化每日学习任务推送
模拟考试+成绩预测
语音口语练习与评分
学习进度可视化报告',
    '竞品A：题库大但无个性化推荐 → 我们有AI自适应引擎
竞品B：价格高 → 我们采用订阅制，性价比更高
我们的优势：团队有教育+AI背景，已获得某高校试点合作',
    '基础功能免费，高级功能月费19元，支持学期/年费套餐',
    '中国大学生人数超3000万，每年四六级考生约1000万人次，备考工具市场规模预计2025年达50亿元。',
    '需要技术合伙人一起开发后端
寻求天使投资50万，出让10%股权
希望接入某平台API资源',
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',
    'education', 'mvp', 2, true, true,
    128, 1520, 23
);

-- 项目2: 碳足迹追踪器
INSERT INTO public.innovations (
    id, title, description, elevator_pitch, problem, solution,
    target_audience, product_type, key_features, competitive_advantage,
    business_model, market_opportunity, ask,
    creator_id, category, stage, team_size, is_public, is_featured,
    like_count, view_count, comment_count
) VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567002',
    '碳足迹追踪器',
    '帮助个人和企业追踪和减少碳排放的智能工具',
    '让每个人都能轻松追踪自己的碳足迹，并获得个性化的减排建议。',
    '普通消费者很难量化自己的碳排放，缺乏有效工具来追踪和减少环境影响。',
    '开发一款App，通过连接银行账单、出行数据自动计算碳足迹，并提供可行的减排方案和碳积分奖励。',
    '主要用户：关注环保的年轻白领
次要用户：ESG合规企业
用户画像：25-40岁，有环保意识，愿意为可持续生活方式付费',
    'iOS/Android App + 企业版SaaS',
    '自动碳排放计算
个性化减排建议
碳积分兑换系统
社区挑战和排行榜
企业ESG报告生成',
    '市面上缺乏面向C端的碳追踪工具
我们与多家银行达成数据合作
AI驱动的精准减排建议',
    'C端免费+广告，企业版按年订阅',
    '全球碳管理市场预计2030年达1500亿美元，中国碳中和政策推动需求增长。',
    '寻求环保基金投资
希望与银行、航空公司合作数据接入',
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',
    'environment', 'prototype', 3, true, false,
    89, 980, 15
);

-- 项目3: 远程协作白板
INSERT INTO public.innovations (
    id, title, description, elevator_pitch, problem, solution,
    target_audience, product_type, key_features, competitive_advantage,
    business_model, market_opportunity, ask,
    creator_id, category, stage, team_size, is_public, is_featured,
    like_count, view_count, comment_count
) VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567003',
    '远程协作白板',
    '支持AI辅助的实时协作白板工具',
    '比Miro更智能的协作白板，AI自动整理会议笔记和任务分配。',
    '远程团队在头脑风暴和需求讨论时，信息分散、难以追踪行动项。',
    '实时协作白板+AI助手，自动生成会议纪要、提取任务、分配责任人。',
    '主要用户：远程工作团队
次要用户：设计师、产品经理
用户画像：互联网行业从业者，习惯远程协作',
    'Web应用 + 桌面客户端',
    '无限画布实时协作
AI自动生成会议纪要
任务自动提取和分配
与Slack/飞书集成
模板库和组件市场',
    'AI自动化能力是核心差异化
比竞品更注重"会后执行"
中文语境优化更好',
    '免费版限制协作人数，团队版按人头收费$8/月',
    '全球协作软件市场500亿美元，中国市场增速最快。',
    '寻找产品设计师加入
期望获得种子轮融资100万',
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',
    'tech', 'idea', 1, true, false,
    45, 620, 8
);
