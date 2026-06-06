import Link from 'next/link';

export default async function HomePage(props: PageProps<'/[lang]'>) {
  const params = await props.params;
  const lang = params.lang;

  return (
    <div className="flex flex-col justify-center text-center flex-1">
      <h1 className="text-4xl font-bold mb-4">Polymerium</h1>
      <p className="text-lg text-fd-muted-foreground mb-8">
        {lang === 'zh'
          ? '下一代 Minecraft 实例管理器'
          : 'A next-generation Minecraft instance manager'}
      </p>
      <p>
        <Link href={`/${lang}/docs`} className="font-medium underline">
          {lang === 'zh' ? '阅读文档' : 'Read the Docs'}
        </Link>
      </p>
    </div>
  );
}
